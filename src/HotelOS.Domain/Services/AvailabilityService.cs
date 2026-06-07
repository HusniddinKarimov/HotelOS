using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;

namespace HotelOS.Domain.Services;

/// <summary>
/// The heart of hotel booking: deciding whether a room is free for a requested
/// date range. A room can have many reservations over its lifetime; it is only
/// unavailable for dates that <b>overlap</b> an existing active reservation.
/// </summary>
public static class AvailabilityService
{
    /// <summary>Reservations in these states actually hold a room.</summary>
    private static readonly ReservationStatus[] BlockingStatuses =
        { ReservationStatus.Confirmed, ReservationStatus.CheckedIn };

    /// <summary>
    /// Two half-open date ranges [aStart, aEnd) and [bStart, bEnd) overlap if,
    /// and only if, each starts before the other ends:
    ///     aStart &lt; bEnd  AND  bStart &lt; aEnd
    /// Using half-open ranges means a check-out on the same day as the next
    /// guest's check-in is NOT an overlap (one leaves in the morning, the next
    /// arrives in the afternoon) — exactly how real hotels work.
    /// </summary>
    public static bool RangesOverlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        => aStart < bEnd && bStart < aEnd;

    /// <summary>
    /// True when a room is free for [checkIn, checkOut): it is not out of service
    /// and none of its active reservations overlap the requested range.
    /// </summary>
    public static bool IsRoomAvailable(Room room, IEnumerable<Reservation> roomReservations, DateTime checkIn, DateTime checkOut)
    {
        if (room.Status == RoomStatus.Maintenance)
            return false; // room is out of service entirely

        return !roomReservations.Any(r =>
            BlockingStatuses.Contains(r.Status) &&
            RangesOverlap(r.CheckInDate, r.CheckOutDate, checkIn, checkOut));
    }
}
