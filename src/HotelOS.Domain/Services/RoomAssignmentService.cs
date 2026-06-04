using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;

namespace HotelOS.Domain.Services;

/// <summary>
/// The core room-assignment algorithm. Selection order:
///   1. Room type must match the request.
///   2. Only Clean rooms are eligible (Dirty/Cleaning/Maintenance/Occupied excluded).
///   3. Prefer the room cleaned longest ago (even rotation).
///   4. Apply floor preference as a soft filter (fall back to any floor).
///   5. Apply elevator/stair proximity as the final tie-breaker.
/// Returns the best room, or null when none is available.
/// </summary>
public static class RoomAssignmentService
{
    public static Room? FindBestRoom(
        IEnumerable<Room> rooms,
        int requestedRoomTypeId,
        int? floorPreference,
        string? proximityPreference)
    {
        // 1 + 2: hard filters.
        var eligible = rooms
            .Where(r => r.RoomTypeId == requestedRoomTypeId)
            .Where(r => r.Status == RoomStatus.Clean)
            .ToList();

        if (eligible.Count == 0)
            return null;

        // 4: floor preference as a soft filter with fallback.
        if (floorPreference is int floor)
        {
            var onFloor = eligible.Where(r => r.Floor == floor).ToList();
            if (onFloor.Count > 0)
                eligible = onFloor;
        }

        // 3: longest-clean first. 5: proximity tie-break. Then room number for determinism.
        return eligible
            .OrderBy(r => r.LastCleanedAt)
            .ThenByDescending(r => MatchesProximity(r, proximityPreference))
            .ThenBy(r => r.Number)
            .First();
    }

    private static bool MatchesProximity(Room room, string? preference) => preference?.ToLowerInvariant() switch
    {
        "elevator" => room.NearElevator,
        "stairs" => !room.NearElevator,
        _ => false
    };
}
