using HotelOS.Domain.Common;
using HotelOS.Domain.Enums;

namespace HotelOS.Domain.Entities;

/// <summary>A guest's invoice, accumulating charges and payments for a stay.</summary>
public class Bill : BaseEntity
{
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public Guid GuestId { get; set; }

    public BillStatus Status { get; set; } = BillStatus.Open;

    public ICollection<BillItem> Items { get; set; } = new List<BillItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>Sum of all non-discount charges.</summary>
    public decimal Subtotal => Items.Where(i => i.Type != BillItemType.Discount).Sum(i => i.Amount);

    /// <summary>Total of discount lines (stored as positive amounts).</summary>
    public decimal DiscountTotal => Items.Where(i => i.Type == BillItemType.Discount).Sum(i => i.Amount);

    public decimal Total => Subtotal - DiscountTotal;

    public decimal Paid => Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);

    public decimal Balance => Total - Paid;
}
