using HotelOS.Domain.Common;
using HotelOS.Domain.Enums;

namespace HotelOS.Domain.Entities;

/// <summary>A payment recorded against a bill.</summary>
public class Payment : BaseEntity
{
    public Guid BillId { get; set; }
    public Bill Bill { get; set; } = null!;

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}
