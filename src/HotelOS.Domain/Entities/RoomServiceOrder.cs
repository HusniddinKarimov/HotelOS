using HotelOS.Domain.Common;
using HotelOS.Domain.Enums;

namespace HotelOS.Domain.Entities;

/// <summary>A food/beverage order linked to a room; charges flow to the guest bill.</summary>
public class RoomServiceOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public int RoomNumber { get; set; }
    public Guid? GuestId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Received;

    public ICollection<RoomServiceOrderItem> Items { get; set; } = new List<RoomServiceOrderItem>();

    public decimal Total => Items.Sum(i => i.LineTotal);
}
