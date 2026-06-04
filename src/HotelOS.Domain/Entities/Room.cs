using HotelOS.Domain.Common;
using HotelOS.Domain.Enums;

namespace HotelOS.Domain.Entities;

/// <summary>A physical room. <see cref="LastCleanedAt"/> drives the longest-clean rule.</summary>
public class Room : BaseEntity
{
    public int Number { get; set; }
    public int Floor { get; set; }
    public bool NearElevator { get; set; }

    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    public RoomStatus Status { get; set; } = RoomStatus.Clean;

    /// <summary>When the room last became Clean (used for even rotation).</summary>
    public DateTime LastCleanedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The guest currently occupying the room, if any.</summary>
    public Guid? CurrentGuestId { get; set; }
    public Guest? CurrentGuest { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
