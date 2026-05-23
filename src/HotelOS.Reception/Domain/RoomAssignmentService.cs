using HotelOS.Contracts.Models;

namespace HotelOS.Reception.Domain;

/// <summary>
/// Implements the core HotelOS room assignment algorithm. The public method
/// <see cref="Assign"/> is the ABSTRACTION described in Task 2: callers ask for
/// "the best room" and the entire multi-criteria decision runs internally.
///
/// Selection criteria, applied in this exact order:
///   1. Room TYPE must match the booking.
///   2. Room STATUS must be Clean (everything else is excluded).
///   3. FLOOR preference is a soft filter: prefer the requested floor, but
///      fall back to any floor if none match.
///   4. Among the survivors, prefer the room CLEAN THE LONGEST (rotation).
///   5. PROXIMITY preference (elevator/stairs) is the final tie-breaker.
/// </summary>
public static class RoomAssignmentService
{
    /// <summary>
    /// Returns the best matching room, or null if none is available.
    /// Pure function over the supplied snapshot — no shared state is mutated
    /// here, which keeps the algorithm easy to test and reason about.
    /// </summary>
    public static Room? Assign(IReadOnlyCollection<Room> rooms, Guest guest)
    {
        // Step 1 + 2: hard filters — correct type AND currently Clean.
        var eligible = rooms
            .Where(r => r.Type == guest.RequestedType)
            .Where(r => r.Status == RoomStatus.Clean)
            .ToList();

        if (eligible.Count == 0)
            return null; // no room of the right type is clean -> caller handles "no availability"

        // Step 3: floor preference as a soft filter with fallback.
        if (guest.FloorPreference is int wantedFloor)
        {
            var onFloor = eligible.Where(r => r.Floor == wantedFloor).ToList();
            if (onFloor.Count > 0)
                eligible = onFloor; // honour preference; otherwise keep all floors
        }

        // Step 4 (primary sort): longest-clean first.
        // Step 5 (tie-break): proximity preference, then room number for determinism.
        var ranked = eligible
            .OrderBy(r => r.CleanSinceUtc)                       // 4: clean the longest
            .ThenByDescending(r => MatchesProximity(r, guest))   // 5: preferred proximity wins ties
            .ThenBy(r => r.Number);                              // deterministic final tie-break

        return ranked.First();
    }

    /// <summary>True when the room satisfies the guest's proximity request.</summary>
    private static bool MatchesProximity(Room room, Guest guest) => guest.ProximityPreference switch
    {
        "elevator" => room.NearElevator,
        "stairs"   => !room.NearElevator,
        _          => false // no preference -> nothing to prefer, leaves order stable
    };
}
