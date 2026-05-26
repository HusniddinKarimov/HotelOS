using System.Net.WebSockets;
using HotelOS.Contracts.Common;
using HotelOS.Contracts.Messaging;
using HotelOS.Contracts.Models;
using HotelOS.Dashboard.Domain;

// ---------------------------------------------------------------------------
// Operations Dashboard (port 5005)
//   • Serves the browser UI and a live WebSocket feed at /ws.
//   • Requires an access token before any data is sent (authentication).
//   • Subscribes to EVERY broker topic ("*") and rebroadcasts an updated
//     snapshot to all connected browsers in real time.
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5005");
var app = builder.Build();
app.UseSafeErrors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseWebSockets();

var state = new DashboardState();
var hub = new BrowserHub();
var broker = new BrokerClient(ServiceConfig.BrokerUrl, "dashboard");

// --- consume every event off the broker and refresh all browsers -----------
broker.Subscribe(Topics.All, async msg =>
{
    switch (msg.Topic)
    {
        case Topics.RoomAssigned:
            if (msg.PayloadAs<RoomAssignedEvent>() is { } a)
                state.OnRoomAssigned(a.RoomNumber, a.GuestName, a.Type.ToString(), a.Floor);
            break;
        case Topics.RoomVacated:
            if (msg.PayloadAs<RoomVacatedEvent>() is { } v) state.OnRoomVacated(v.RoomNumber);
            break;
        case Topics.HousekeepingStatusChanged:
            if (msg.PayloadAs<HousekeepingStatusEvent>() is { } h) state.OnHousekeeping(h.RoomNumber, h.Status);
            break;
        case Topics.OrderUpdate:
            if (msg.PayloadAs<OrderUpdateEvent>() is { } o) state.OnOrder(o.OrderId, o.RoomNumber, o.Summary, o.Status);
            break;
        case Topics.MaintenanceUpdate:
            if (msg.PayloadAs<MaintenanceUpdateEvent>() is { } m)
                state.OnMaintenance(m.IssueId, m.RoomNumber, m.Description, m.Urgency, m.Status, m.Technician);
            break;
        default:
            return; // ignore charge / checked_out for the live grid
    }
    await hub.BroadcastAsync(state.Snapshot());
});

await broker.StartAsync(app.Lifetime.ApplicationStopping);

// Seed the room grid from Reception so a freshly-started dashboard isn't blank.
_ = SeedRoomsAsync(state);

// --- live WebSocket feed for browsers --------------------------------------
app.Map("/ws", async context =>
{
    // Authentication: a valid token is required before any data is sent.
    var token = context.Request.Query["token"].ToString();
    if (token != ServiceConfig.DashboardToken)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var id = Guid.NewGuid();
    hub.Add(id, socket);
    await hub.SendAsync(socket, state.Snapshot()); // initial state

    var buffer = new byte[1024];
    try
    {
        while (socket.State == WebSocketState.Open)
        {
            var r = await socket.ReceiveAsync(buffer, context.RequestAborted);
            if (r.MessageType == WebSocketMessageType.Close) break;
        }
    }
    catch { /* client went away */ }
    finally { hub.Remove(id); }
});

Console.WriteLine("Dashboard listening on http://localhost:5005  (token: " + ServiceConfig.DashboardToken + ")");
app.Run();

// Poll Reception for the room inventory until it answers (services may start in any order).
static async Task SeedRoomsAsync(DashboardState state)
{
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    for (var attempt = 0; attempt < 20; attempt++)
    {
        try
        {
            var rooms = await http.GetFromJsonAsync<List<Room>>("http://localhost:5001/rooms");
            if (rooms is not null) { state.SeedRooms(rooms); return; }
        }
        catch { /* Reception not up yet */ }
        await Task.Delay(1000);
    }
}
