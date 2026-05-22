using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using HotelOS.Contracts.Messaging;

// ---------------------------------------------------------------------------
// HotelOS Message Broker
//
// A simplified but genuine topic-based publish/subscribe broker. Services open
// a WebSocket to /broker, send {action:"subscribe",topic} frames to register
// interest, and {action:"publish",topic,payload} frames to broadcast. The
// broker forwards each published message to every subscriber of that topic
// (and every wildcard "*" subscriber, used by the dashboard). Publishers never
// know who — if anyone — is listening. This is what decouples the services.
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
var app = builder.Build();

app.UseWebSockets();

var hub = new SubscriptionHub();

app.MapGet("/", () => "HotelOS Broker is running. Connect a WebSocket to /broker.");
app.MapGet("/health", () => Results.Ok(new { status = "up", topics = hub.TopicCounts() }));

app.Map("/broker", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var id = Guid.NewGuid();
    Console.WriteLine($"[broker] client {id} connected");

    try
    {
        await ReceiveLoop(id, socket, hub);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[broker] client {id} error: {ex.Message}");
    }
    finally
    {
        hub.RemoveEverywhere(id);
        Console.WriteLine($"[broker] client {id} disconnected");
    }
});

Console.WriteLine("HotelOS Broker listening on http://localhost:5000 (ws://localhost:5000/broker)");
app.Run();

// --- helpers ---------------------------------------------------------------

static async Task ReceiveLoop(Guid id, WebSocket socket, SubscriptionHub hub)
{
    var buffer = new byte[8192];
    while (socket.State == WebSocketState.Open)
    {
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                return;
            }
            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        var text = Encoding.UTF8.GetString(ms.ToArray());
        BrokerMessage? msg;
        try { msg = JsonSerializer.Deserialize<BrokerMessage>(text, Json.Options); }
        catch { continue; } // ignore malformed frames rather than crashing

        if (msg is null) continue;

        switch (msg.Action)
        {
            case "subscribe":
                hub.Subscribe(msg.Topic, id, socket);
                Console.WriteLine($"[broker] client {id} subscribed to '{msg.Topic}'");
                break;
            case "publish":
                var delivered = await hub.PublishAsync(msg);
                Console.WriteLine($"[broker] '{msg.Topic}' -> {delivered} subscriber(s)");
                break;
        }
    }
}

/// <summary>Thread-safe registry of which sockets are subscribed to which topic.</summary>
sealed class SubscriptionHub
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> _topics = new();

    public void Subscribe(string topic, Guid id, WebSocket socket)
    {
        var set = _topics.GetOrAdd(topic, _ => new ConcurrentDictionary<Guid, WebSocket>());
        set[id] = socket;
    }

    public async Task<int> PublishAsync(BrokerMessage msg)
    {
        var targets = new List<WebSocket>();
        if (_topics.TryGetValue(msg.Topic, out var subs)) targets.AddRange(subs.Values);
        if (_topics.TryGetValue(Topics.All, out var wild)) targets.AddRange(wild.Values);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(msg, Json.Options);
        var count = 0;
        foreach (var s in targets)
        {
            if (s.State != WebSocketState.Open) continue;
            try
            {
                await s.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                count++;
            }
            catch { /* a slow/dead subscriber must not block the others */ }
        }
        return count;
    }

    public void RemoveEverywhere(Guid id)
    {
        foreach (var set in _topics.Values) set.TryRemove(id, out _);
    }

    public Dictionary<string, int> TopicCounts() =>
        _topics.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
}
