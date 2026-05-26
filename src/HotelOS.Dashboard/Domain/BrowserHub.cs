using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using HotelOS.Contracts.Messaging;

namespace HotelOS.Dashboard.Domain;

/// <summary>
/// Tracks the browser WebSocket connections and pushes live updates to them.
/// This is the server-to-client half of the real-time dashboard.
/// </summary>
public sealed class BrowserHub
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();

    public void Add(Guid id, WebSocket socket) => _clients[id] = socket;
    public void Remove(Guid id) => _clients.TryRemove(id, out _);

    /// <summary>Push an object as JSON to one specific client (e.g. initial snapshot).</summary>
    public Task SendAsync(WebSocket socket, object payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Json.Options);
        return socket.State == WebSocketState.Open
            ? socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None)
            : Task.CompletedTask;
    }

    /// <summary>Broadcast an object as JSON to every connected browser.</summary>
    public async Task BroadcastAsync(object payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Json.Options);
        foreach (var (id, socket) in _clients)
        {
            if (socket.State != WebSocketState.Open) { _clients.TryRemove(id, out _); continue; }
            try { await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None); }
            catch { _clients.TryRemove(id, out _); }
        }
    }
}
