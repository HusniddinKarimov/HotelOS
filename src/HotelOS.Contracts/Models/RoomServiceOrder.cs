namespace HotelOS.Contracts.Models;

/// <summary>A single food/drink line on a room-service order.</summary>
public record OrderItem(string Name, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

/// <summary>
/// A room-service order linked to a room. Moves through the
/// <see cref="OrderStatus"/> state machine; each transition is published to
/// the broker so the dashboard updates live.
/// </summary>
public class RoomServiceOrder
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..6];
    public int RoomNumber { get; init; }
    public List<OrderItem> Items { get; init; } = new();
    public OrderStatus Status { get; set; } = OrderStatus.Received;
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    public decimal Total => Items.Sum(i => i.LineTotal);
}
