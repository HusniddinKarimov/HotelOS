using HotelOS.Domain.Entities;

namespace HotelOS.Application.Features.Orders;

public record OrderItemDto(string Name, int Quantity, decimal UnitPrice, decimal LineTotal);

public record OrderDto(
    Guid Id,
    string OrderNumber,
    int RoomNumber,
    string Status,
    IReadOnlyList<OrderItemDto> Items,
    decimal Total,
    DateTime CreatedAt);

public static class OrderMapping
{
    public static OrderDto ToDto(this RoomServiceOrder o) => new(
        o.Id,
        o.OrderNumber,
        o.RoomNumber,
        o.Status.ToString(),
        o.Items.Select(i => new OrderItemDto(i.Name, i.Quantity, i.UnitPrice, i.LineTotal)).ToList(),
        o.Total,
        o.CreatedAt);
}
