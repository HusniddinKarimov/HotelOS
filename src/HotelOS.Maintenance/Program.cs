using HotelOS.Contracts.Common;
using HotelOS.Contracts.Messaging;
using HotelOS.Contracts.Models;
using HotelOS.Maintenance.Domain;

// ---------------------------------------------------------------------------
// Maintenance Service (port 5004)
//   • Accepts issue reports with an urgency level.
//   • A binary-heap priority queue ranks issues; free technicians are assigned
//     highest-priority first (FIFO tie-break on equal urgency).
//   • Publishes: maintenance.issue_update on every state change.
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5004");
var app = builder.Build();
app.UseSafeErrors();

var coordinator = new MaintenanceCoordinator();
var broker = new BrokerClient(ServiceConfig.BrokerUrl, "maintenance");
await broker.StartAsync(app.Lifetime.ApplicationStopping);

app.MapGet("/", () => "Maintenance Service up.");
app.MapGet("/issues", () => Results.Ok(coordinator.Snapshot()));

// Report a fault. Validates room, description and urgency.
app.MapPost("/issues", async (IssueRequest req) =>
{
    Validation.RequireRoomNumber(req.RoomNumber);
    var description = Validation.RequireText(req.Description, "Description");
    if (!Enum.TryParse<Urgency>(req.Urgency, ignoreCase: true, out var urgency))
        throw new ValidationException("Urgency must be Critical, High, Normal or Low.");

    var changed = coordinator.Report(req.RoomNumber, description, urgency);
    await PublishAll(broker, changed);

    var reported = changed[0];
    return Results.Ok(new
    {
        issueId = reported.Id,
        status = reported.Status.ToString(),
        technician = reported.AssignedTechnician
    });
});

// Technician marks an issue complete.
app.MapPost("/issues/{id}/resolve", async (string id) =>
{
    var changed = coordinator.Resolve(id);
    if (changed is null)
        throw new ValidationException($"Issue {id} not found or already resolved.");

    await PublishAll(broker, changed);
    return Results.Ok(new { issueId = id, status = "Resolved" });
});

Console.WriteLine("Maintenance Service listening on http://localhost:5004");
app.Run();

// Publish a broker event for every issue whose state changed.
static async Task PublishAll(BrokerClient broker, List<MaintenanceIssue> issues)
{
    foreach (var i in issues)
        await broker.PublishAsync(Topics.MaintenanceUpdate,
            new MaintenanceUpdateEvent(i.Id, i.RoomNumber, i.Description, i.Urgency, i.Status, i.AssignedTechnician));
}

record IssueRequest(int RoomNumber, string Description, string Urgency);
