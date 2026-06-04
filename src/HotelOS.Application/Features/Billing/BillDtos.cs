using HotelOS.Domain.Entities;

namespace HotelOS.Application.Features.Billing;

public record BillItemDto(Guid Id, string Description, string Type, decimal Amount, int Quantity);

public record BillDto(
    Guid Id,
    Guid ReservationId,
    string Status,
    IReadOnlyList<BillItemDto> Items,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    decimal Paid,
    decimal Balance);

public static class BillMapping
{
    public static BillDto ToDto(this Bill b) => new(
        b.Id,
        b.ReservationId,
        b.Status.ToString(),
        b.Items.OrderBy(i => i.CreatedAt)
               .Select(i => new BillItemDto(i.Id, i.Description, i.Type.ToString(), i.Amount, i.Quantity))
               .ToList(),
        b.Subtotal,
        b.DiscountTotal,
        b.Total,
        b.Paid,
        b.Balance);
}
