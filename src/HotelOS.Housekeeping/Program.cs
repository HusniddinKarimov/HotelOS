using HotelOS.Contracts.Common;
using HotelOS.Contracts.Messaging;
using HotelOS.Contracts.Models;
using HotelOS.Housekeeping.Domain;

// ---------------------------------------------------------------------------
// Housekeeping Service (port 5002)
//   • Subscribes: reception.room_vacated -> add room to the cleaning queue.
//   • Publishes:  housekeeping.status_changed (BeingCleaned, then Clean).
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5002");
var app = builder.Build();
app.UseSafeErrors();

var board = new CleaningBoard();
var broker = new BrokerClient(ServiceConfig.BrokerUrl, "housekeeping");

// Event-driven inbound: a vacated room joins the cleaning queue automatically.
broker.Subscribe(Topics.RoomVacated, msg =>
{
    var e = msg.PayloadAs<RoomVacatedEvent>();
    if (e is not null && board.Enqueue(e.RoomNumber))
        Console.WriteLine($"[housekeeping] room {e.RoomNumber} queued for cleaning");
    return Task.CompletedTask;
});

await broker.StartAsync(app.Lifetime.ApplicationStopping);

app.MapGet("/", () => "Housekeeping Service up.");
app.MapGet("/queue", () => Results.Ok(board.Snapshot()));

// Housekeeper starts cleaning a room -> status BeingCleaned.
app.MapPost("/clean/start", async (RoomRef req) =>
{
    Validation.RequireRoomNumber(req.RoomNumber);
    if (!board.StartCleaning(req.RoomNumber))
        throw new ValidationException($"Room {req.RoomNumber} is not in the cleaning queue.");

    await broker.PublishAsync(Topics.HousekeepingStatusChanged,
        new HousekeepingStatusEvent(req.RoomNumber, RoomStatus.BeingCleaned));
    return Results.Ok(new { room = req.RoomNumber, status = "BeingCleaned" });
});

// Housekeeper finishes -> status Clean, room becomes assignable again.
app.MapPost("/clean/done", async (RoomRef req) =>
{
    Validation.RequireRoomNumber(req.RoomNumber);
    if (!board.FinishCleaning(req.RoomNumber))
        throw new ValidationException($"Room {req.RoomNumber} is not currently being cleaned.");

    await broker.PublishAsync(Topics.HousekeepingStatusChanged,
        new HousekeepingStatusEvent(req.RoomNumber, RoomStatus.Clean));
    return Results.Ok(new { room = req.RoomNumber, status = "Clean" });
});

Console.WriteLine("Housekeeping Service listening on http://localhost:5002");
app.Run();

record RoomRef(int RoomNumber);
