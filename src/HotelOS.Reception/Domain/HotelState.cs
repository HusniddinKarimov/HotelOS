using HotelOS.Contracts.Models;

namespace HotelOS.Reception.Domain;

/// <summary>
/// The authoritative store of room inventory and guest records owned by the
/// Reception service. Demonstrates ENCAPSULATION: the room list and guest map
/// are private and can only be changed through guarded methods, so no external
/// code can corrupt hotel state.
///
/// DATA STRUCTURES (justified in the report):
///   • List&lt;Room&gt;  — the room inventory: a small, fixed, index-friendly set.
///   • Dictionary&lt;string,Guest&gt; — guest records keyed by id for O(1) lookup.
///
/// A single lock serialises assignment and checkout so two simultaneous
/// check-ins can never be given the same room (test scenario TS-06).
/// </summary>
public sealed class HotelState
{
    private readonly List<Room> _rooms;
    private readonly Dictionary<string, Guest> _guests = new();
    private readonly object _gate = new();

    public HotelState()
    {
        // 10 rooms across 2 floors. CleanSinceUtc is staggered so the
        // "longest clean" rule has something meaningful to order by.
        var now = DateTime.UtcNow;
        _rooms = new List<Room>
        {
            Room(101, 1, RoomType.Single,     80m, near: true,  cleanAgoMins: 200, now),
            Room(102, 1, RoomType.Double,    120m, near: true,  cleanAgoMins: 60,  now),
            Room(103, 1, RoomType.Double,    120m, near: false, cleanAgoMins: 300, now),
            Room(104, 1, RoomType.Suite,     250m, near: false, cleanAgoMins: 120, now),
            Room(105, 1, RoomType.Accessible,110m, near: false, cleanAgoMins: 90,  now),
            Room(201, 2, RoomType.Single,     80m, near: true,  cleanAgoMins: 30,  now),
            Room(202, 2, RoomType.Double,    120m, near: true,  cleanAgoMins: 240, now),
            Room(203, 2, RoomType.Double,    120m, near: false, cleanAgoMins: 15,  now),
            Room(204, 2, RoomType.Suite,     250m, near: false, cleanAgoMins: 180, now),
            Room(205, 2, RoomType.Accessible,110m, near: false, cleanAgoMins: 45,  now),
        };
    }

    private static Room Room(int number, int floor, RoomType type, decimal rate, bool near, int cleanAgoMins, DateTime now) =>
        new()
        {
            Number = number, Floor = floor, Type = type, NightlyRate = rate,
            NearElevator = near, Status = RoomStatus.Clean,
            CleanSinceUtc = now.AddMinutes(-cleanAgoMins)
        };

    /// <summary>A read-only copy of the inventory, safe to expose to the dashboard.</summary>
    public List<Room> Snapshot()
    {
        lock (_gate)
            return _rooms.Select(Clone).ToList();
    }

    public Guest? GetGuest(string id)
    {
        lock (_gate) return _guests.GetValueOrDefault(id);
    }

    /// <summary>
    /// Atomically runs the assignment algorithm and reserves the chosen room.
    /// The lock is what prevents the TS-06 double-booking race condition.
    /// Returns the assigned room (a copy) or null if nothing was available.
    /// </summary>
    public Room? AssignRoom(Guest guest)
    {
        lock (_gate)
        {
            var chosen = RoomAssignmentService.Assign(_rooms, guest);
            if (chosen is null) return null;

            chosen.Status = RoomStatus.Occupied;
            chosen.OccupiedByGuestId = guest.Id;
            guest.AssignedRoom = chosen.Number;
            _guests[guest.Id] = guest;
            return Clone(chosen);
        }
    }

    /// <summary>
    /// Settles the bill for the guest in a room and marks the room Dirty.
    /// Returns null if the room is not currently occupied.
    /// </summary>
    public (Bill bill, Guest guest)? Checkout(int roomNumber, decimal discountRate)
    {
        lock (_gate)
        {
            var room = _rooms.FirstOrDefault(r => r.Number == roomNumber);
            if (room is null || room.Status != RoomStatus.Occupied || room.OccupiedByGuestId is null)
                return null;

            var guest = _guests[room.OccupiedByGuestId];
            var bill = BillingService.Calculate(room, guest, discountRate);

            room.Status = RoomStatus.Dirty;
            room.OccupiedByGuestId = null;
            return (bill, guest);
        }
    }

    /// <summary>Applies a housekeeping status change received from the broker.</summary>
    public void ApplyHousekeepingStatus(int roomNumber, RoomStatus status)
    {
        lock (_gate)
        {
            var room = _rooms.FirstOrDefault(r => r.Number == roomNumber);
            if (room is null) return;
            room.Status = status;
            if (status == RoomStatus.Clean)
                room.CleanSinceUtc = DateTime.UtcNow; // resets rotation clock
        }
    }

    /// <summary>Posts a charge against whichever guest occupies the room.</summary>
    public bool AddCharge(int roomNumber, string description, decimal amount)
    {
        lock (_gate)
        {
            var room = _rooms.FirstOrDefault(r => r.Number == roomNumber);
            if (room?.OccupiedByGuestId is null) return false;
            _guests[room.OccupiedByGuestId].Charges.Add(new Charge(description, amount, DateTime.UtcNow));
            return true;
        }
    }

    /// <summary>Marks a room under maintenance (or restores it to Clean on resolve).</summary>
    public void SetMaintenance(int roomNumber, bool underMaintenance)
    {
        lock (_gate)
        {
            var room = _rooms.FirstOrDefault(r => r.Number == roomNumber);
            if (room is null || room.Status == RoomStatus.Occupied) return;
            room.Status = underMaintenance ? RoomStatus.Maintenance : RoomStatus.Clean;
            if (!underMaintenance) room.CleanSinceUtc = DateTime.UtcNow;
        }
    }

    private static Room Clone(Room r) => new()
    {
        Number = r.Number, Floor = r.Floor, Type = r.Type, NightlyRate = r.NightlyRate,
        NearElevator = r.NearElevator, Status = r.Status,
        CleanSinceUtc = r.CleanSinceUtc, OccupiedByGuestId = r.OccupiedByGuestId
    };
}
