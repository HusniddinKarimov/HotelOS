using HotelOS.Domain.Common;

namespace HotelOS.Domain.Entities;

/// <summary>A single line item on a room-service order.</summary>
public class RoomServiceOrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public RoomServiceOrder Order { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;
}
