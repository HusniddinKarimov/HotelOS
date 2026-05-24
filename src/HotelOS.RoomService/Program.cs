using HotelOS.Contracts.Common;
using HotelOS.Contracts.Messaging;
using HotelOS.Contracts.Models;
using HotelOS.RoomService.Domain;

// ---------------------------------------------------------------------------
// Room Service (port 5003)
//   • Accepts food/drink orders linked to a room.
//   • Orders move Received -> Preparing -> OutForDelivery -> Delivered.
//   • Publishes: roomservice.order_update (every transition) and
//                roomservice.charge (posts the cost to the room's bill).
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5003");
var app = builder.Build();
app.UseSafeErrors();

var board = new OrderBoard();
var broker = new BrokerClient(ServiceConfig.BrokerUrl, "roomservice");
await broker.StartAsync(app.Lifetime.ApplicationStopping);

app.MapGet("/", () => "Room Service up.");
app.MapGet("/orders", () => Results.Ok(board.Active()));

// Place an order. Validates the room and every line item.
app.MapPost("/orders", async (NewOrderRequest req) =>
{
    Validation.RequireRoomNumber(req.RoomNumber);
    if (req.Items is null || req.Items.Count == 0)
        throw new ValidationException("An order must contain at least one item.");

    var items = req.Items.Select(i => new OrderItem(
        Validation.RequireText(i.Name, "Item name", 60),
        Validation.RequirePositiveQty(i.Quantity),
        i.UnitPrice < 0 ? throw new ValidationException("Price cannot be negative.") : i.UnitPrice
    )).ToList();

    var order = new RoomServiceOrder { RoomNumber = req.RoomNumber, Items = items };
    board.Add(order);

    var summary = string.Join(", ", items.Select(i => $"{i.Quantity}× {i.Name}"));

    // Announce the new order and post the charge to the room's bill.
    await broker.PublishAsync(Topics.OrderUpdate,
        new OrderUpdateEvent(order.Id, order.RoomNumber, summary, order.Status));
    await broker.PublishAsync(Topics.OrderCharge,
        new OrderChargeEvent(order.RoomNumber, $"Room service #{order.Id}: {summary}", order.Total));

    return Results.Ok(new { orderId = order.Id, status = order.Status.ToString(), total = order.Total });
});

// Advance an order to its next state.
app.MapPost("/orders/{id}/advance", async (string id) =>
{
    var order = board.Advance(id);
    if (order is null)
        throw new ValidationException($"Order {id} not found or already delivered.");

    var summary = string.Join(", ", order.Items.Select(i => $"{i.Quantity}× {i.Name}"));
    await broker.PublishAsync(Topics.OrderUpdate,
        new OrderUpdateEvent(order.Id, order.RoomNumber, summary, order.Status));

    return Results.Ok(new { orderId = order.Id, status = order.Status.ToString() });
});

Console.WriteLine("Room Service listening on http://localhost:5003");
app.Run();

record NewOrderRequest(int RoomNumber, List<OrderItemDto> Items);
record OrderItemDto(string Name, int Quantity, decimal UnitPrice);
