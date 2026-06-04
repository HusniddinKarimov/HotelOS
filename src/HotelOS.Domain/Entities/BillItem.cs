using HotelOS.Domain.Common;
using HotelOS.Domain.Enums;

namespace HotelOS.Domain.Entities;

/// <summary>A single line on a bill (room night block, food, minibar, discount...).</summary>
public class BillItem : BaseEntity
{
    public Guid BillId { get; set; }
    public Bill Bill { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public BillItemType Type { get; set; }
    public decimal Amount { get; set; }
    public int Quantity { get; set; } = 1;
}
