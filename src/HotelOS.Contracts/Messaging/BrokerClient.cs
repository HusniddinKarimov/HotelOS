using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace HotelOS.Contracts.Messaging;

/// <summary>
/// A reusable client every microservice uses to talk to the message broker.
/// It hides all WebSocket plumbing behind two operations — Subscribe and
/// Publish — so services stay completely decoupled from one another
/// (ABSTRACTION). It auto-reconnects, so services can start in any order.
/// </summary>
public sealed class BrokerClient : IAsyncDisposable
{
    private readonly Uri _uri;
    private readonly string _serviceName;
    private readonly ConcurrentDictionary<string, Func<BrokerMessage, Task>> _handlers = new();
    private ClientWebSocket _socket = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private CancellationTokenSource _cts = new();
    private volatile bool _connected;

    public BrokerClient(string brokerUrl, string serviceName)
    {
        _uri = new Uri(brokerUrl);
        _serviceName = serviceName;
    }

    /// <summary>Register a handler for a topic. Safe to call before connecting.</summary>
    public void Subscribe(string topic, Func<BrokerMessage, Task> handler)
    {
        _handlers[topic] = handler;
    }

    /// <summary>Publish a strongly-typed event body to a topic.</summary>
    public Task PublishAsync<T>(string topic, T body) =>
        SendAsync(BrokerMessage.Publish(topic, body));

    /// <summary>Open the connection and start the background receive loop.</summary>
    public async Task StartAsync(CancellationToken appStopping)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(appStopping);
        _ = Task.Run(() => ConnectLoopAsync(_cts.Token));
        // Give the first connection a moment so early publishes are not lost.
        for (var i = 0; i < 50 && !_connected; i++)
            await Task.Delay(100, appStopping);
    }

    private async Task ConnectLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _socket = new ClientWebSocket();
                await _socket.ConnectAsync(_uri, ct);
                _connected = true;
                Console.WriteLine($"[{_serviceName}] connected to broker at {_uri}");

                // Re-send every subscription on (re)connect.
                foreach (var topic in _handlers.Keys)
                    await SendRawAsync(BrokerMessage.Subscribe(topic), ct);

                await ReceiveLoopAsync(ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                Console.WriteLine($"[{_serviceName}] broker connection lost: {ex.Message}. Retrying in 1s...");
            }

            _connected = false;
            if (!ct.IsCancellationRequested)
                await Task.Delay(1000, ct);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        while (_socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await _socket.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    return;
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            var text = Encoding.UTF8.GetString(ms.ToArray());
            var msg = JsonSerializer.Deserialize<BrokerMessage>(text, Json.Options);
            if (msg is null) continue;

            if (_handlers.TryGetValue(msg.Topic, out var handler))
                await SafeInvoke(handler, msg);
            else if (_handlers.TryGetValue(Topics.All, out var wildcard))
                await SafeInvoke(wildcard, msg);
        }
    }

    private async Task SafeInvoke(Func<BrokerMessage, Task> handler, BrokerMessage msg)
    {
        try { await handler(msg); }
        catch (Exception ex)
        {
            // A failing handler must never tear down the receive loop.
            Console.WriteLine($"[{_serviceName}] handler error on '{msg.Topic}': {ex.Message}");
        }
    }

    private async Task SendAsync(BrokerMessage msg)
    {
        // Wait briefly for connectivity rather than dropping the message.
        for (var i = 0; i < 50 && !_connected; i++)
            await Task.Delay(100);
        await SendRawAsync(msg, _cts.Token);
    }

    private async Task SendRawAsync(BrokerMessage msg, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(msg, Json.Options);
        await _sendLock.WaitAsync(ct);
        try
        {
            if (_socket.State == WebSocketState.Open)
                await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
        }
        finally { _sendLock.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        if (_socket.State == WebSocketState.Open)
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        _socket.Dispose();
    }
}
