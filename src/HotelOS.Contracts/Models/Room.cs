namespace HotelOS.Contracts.Models;

/// <summary>
/// A physical hotel room. This is the unit the room assignment algorithm
/// selects from. <see cref="CleanSinceUtc"/> drives the "longest clean"
/// rotation rule, and <see cref="NearElevator"/> drives the proximity
/// tie-breaker.
/// </summary>
public class Room
{
    public int Number { get; init; }
    public int Floor { get; init; }
    public RoomType Type { get; init; }

    /// <summary>Nightly rate in GBP. Used by the billing algorithm.</summary>
    public decimal NightlyRate { get; init; }

    /// <summary>True if the room is adjacent to the elevator (proximity tie-breaker).</summary>
    public bool NearElevator { get; init; }

    public RoomStatus Status { get; set; } = RoomStatus.Clean;

    /// <summary>
    /// The UTC instant the room last became Clean. The assignment algorithm
    /// prioritises the room that has been clean the longest (smallest value).
    /// </summary>
    public DateTime CleanSinceUtc { get; set; } = DateTime.UtcNow;

    /// <summary>The id of the guest currently occupying the room, if any.</summary>
    public string? OccupiedByGuestId { get; set; }
}
