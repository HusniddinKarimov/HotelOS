using HotelOS.Contracts.Common;
using HotelOS.Contracts.Messaging;
using HotelOS.Contracts.Models;
using HotelOS.Reception.Domain;

// ---------------------------------------------------------------------------
// Reception Service (port 5001)
//   • Owns the room inventory and guest records.
//   • Runs the room assignment algorithm on check-in.
//   • Runs the billing algorithm on check-out.
//   • Publishes: room_assigned, room_vacated, checked_out.
//   • Subscribes: housekeeping.status_changed, roomservice.charge,
//                 maintenance.issue_update.
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5001");
var app = builder.Build();
app.UseSafeErrors();

var state = new HotelState();
var broker = new BrokerClient(ServiceConfig.BrokerUrl, "reception");

// --- broker subscriptions (event-driven inbound) ---------------------------

// Housekeeping moved a room along its clean lifecycle -> update our inventory.
broker.Subscribe(Topics.HousekeepingStatusChanged, msg =>
{
    var e = msg.PayloadAs<HousekeepingStatusEvent>();
    if (e is not null) state.ApplyHousekeepingStatus(e.RoomNumber, e.Status);
    return Task.CompletedTask;
});

// Room service posted a charge -> add it to the occupying guest's bill.
broker.Subscribe(Topics.OrderCharge, msg =>
{
    var e = msg.PayloadAs<OrderChargeEvent>();
    if (e is not null) state.AddCharge(e.RoomNumber, e.Description, e.Amount);
    return Task.CompletedTask;
});

// Maintenance opened/resolved an issue -> flag/restore the room.
broker.Subscribe(Topics.MaintenanceUpdate, msg =>
{
    var e = msg.PayloadAs<MaintenanceUpdateEvent>();
    if (e is not null)
        state.SetMaintenance(e.RoomNumber, underMaintenance: e.Status != IssueStatus.Resolved);
    return Task.CompletedTask;
});

await broker.StartAsync(app.Lifetime.ApplicationStopping);

// --- HTTP API --------------------------------------------------------------

app.MapGet("/", () => "Reception Service up.");
app.MapGet("/rooms", () => Results.Ok(state.Snapshot()));

// Check-in: validate, build the guest, run assignment, announce the result.
app.MapPost("/checkin", async (CheckInRequest req) =>
{
    var name = Validation.RequireName(req.GuestName);
    var nights = Validation.RequireNights(req.Nights);
    if (!Enum.TryParse<RoomType>(req.RoomType, ignoreCase: true, out var type))
        throw new ValidationException("Room type must be Single, Double, Suite or Accessible.");

    var guest = new Guest
    {
        Name = name,
        RequestedType = type,
        FloorPreference = req.FloorPreference,
        ProximityPreference = req.Proximity?.ToLowerInvariant(),
        Nights = nights,
        CardNumber = req.CardNumber
    };

    var room = state.AssignRoom(guest);
    if (room is null)
    {
        // TS-07: no crash — return a clear message plus alternatives.
        var alternatives = state.Snapshot()
            .Where(r => r.Status == RoomStatus.Clean)
            .Select(r => r.Type.ToString())
            .Distinct()
            .ToList();
        return Results.Ok(new
        {
            assigned = false,
            message = $"No {type} rooms are currently available.",
            alternativeTypes = alternatives,
            waitlist = true
        });
    }

    // Announce to the rest of the hotel — note: NO card data on the wire.
    await broker.PublishAsync(Topics.RoomAssigned,
        new RoomAssignedEvent(room.Number, guest.Id, guest.Name, room.Type, room.Floor));

    return Results.Ok(new
    {
        assigned = true,
        guestId = guest.Id,
        room = room.Number,
        floor = room.Floor,
        type = room.Type.ToString(),
        nearElevator = room.NearElevator
    });
});

// Check-out: validate, settle the bill, mark dirty, publish events.
app.MapPost("/checkout", async (CheckOutRequest req) =>
{
    Validation.RequireRoomNumber(req.RoomNumber);

    var result = state.Checkout(req.RoomNumber, req.DiscountRate ?? 0m);
    if (result is null)
        throw new ValidationException($"Room {req.RoomNumber} is not currently occupied.");

    var (bill, guest) = result.Value;

    // Tell housekeeping the room is now empty and needs cleaning.
    await broker.PublishAsync(Topics.RoomVacated, new RoomVacatedEvent(req.RoomNumber));
    // Dashboard summary only — name + total, never card details.
    await broker.PublishAsync(Topics.CheckedOut,
        new CheckedOutEvent(req.RoomNumber, guest.Name, bill.Total));

    return Results.Ok(bill);
});

Console.WriteLine("Reception Service listening on http://localhost:5001");
app.Run();

// --- request DTOs ----------------------------------------------------------
record CheckInRequest(string GuestName, string RoomType, int Nights, int? FloorPreference, string? Proximity, string? CardNumber);
record CheckOutRequest(int RoomNumber, decimal? DiscountRate);
