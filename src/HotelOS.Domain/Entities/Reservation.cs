using HotelOS.Domain.Common;
using HotelOS.Domain.Enums;

namespace HotelOS.Domain.Entities;

/// <summary>A booking for a guest. A room is assigned at creation or at check-in.</summary>
public class Reservation : BaseEntity
{
    public string ReferenceCode { get; set; } = string.Empty;

    public Guid GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    /// <summary>The requested room category.</summary>
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    /// <summary>The assigned physical room (null until assigned).</summary>
    public Guid? RoomId { get; set; }
    public Room? Room { get; set; }

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public DateTime? ActualCheckInAt { get; set; }
    public DateTime? ActualCheckOutAt { get; set; }

    public int? FloorPreference { get; set; }
    public string? ProximityPreference { get; set; } // "elevator" | "stairs" | null

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public Bill? Bill { get; set; }

    /// <summary>Number of nights (minimum 1).</summary>
    public int Nights => Math.Max(1, (CheckOutDate.Date - CheckInDate.Date).Days);
}
