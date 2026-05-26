using HotelOS.Contracts.Models;

namespace HotelOS.Dashboard.Domain;

/// <summary>
/// The dashboard's own aggregated, read-only view of the hotel, rebuilt purely
/// from events arriving on the broker (plus an initial HTTP seed). It NEVER
/// stores sensitive guest data — only the guest name needed for display.
/// </summary>
public sealed class DashboardState
{
    public sealed class RoomView
    {
        public int Number { get; set; }
        public int Floor { get; set; }
        public string Type { get; set; } = "";
        public string Status { get; set; } = "Clean";
        public string? Guest { get; set; }
    }

    private readonly Dictionary<int, RoomView> _rooms = new();
    private readonly Dictionary<string, object> _orders = new();
    private readonly Dictionary<string, object> _issues = new();
    private readonly object _gate = new();

    /// <summary>Seed room inventory from Reception's HTTP snapshot at startup.</summary>
    public void SeedRooms(IEnumerable<Room> rooms)
    {
        lock (_gate)
            foreach (var r in rooms)
                _rooms[r.Number] = new RoomView
                {
                    Number = r.Number, Floor = r.Floor,
                    Type = r.Type.ToString(), Status = r.Status.ToString()
                };
    }

    public void OnRoomAssigned(int room, string guest, string type, int floor)
    {
        lock (_gate)
        {
            var view = Get(room, type, floor);
            view.Status = nameof(RoomStatus.Occupied);
            view.Guest = guest;
        }
    }

    public void OnRoomVacated(int room)
    {
        lock (_gate)
        {
            if (_rooms.TryGetValue(room, out var v)) { v.Status = nameof(RoomStatus.Dirty); v.Guest = null; }
        }
    }

    public void OnHousekeeping(int room, RoomStatus status)
    {
        lock (_gate)
            if (_rooms.TryGetValue(room, out var v)) v.Status = status.ToString();
    }

    public void OnMaintenance(string id, int room, string desc, Urgency urgency, IssueStatus status, string? tech)
    {
        lock (_gate)
        {
            if (status == IssueStatus.Resolved) _issues.Remove(id);
            else _issues[id] = new { id, room, description = desc, urgency = urgency.ToString(), status = status.ToString(), technician = tech };

            // Reflect maintenance on the room tile when not occupied.
            if (_rooms.TryGetValue(room, out var v) && v.Status != nameof(RoomStatus.Occupied))
                v.Status = status == IssueStatus.Resolved ? nameof(RoomStatus.Clean) : nameof(RoomStatus.Maintenance);
        }
    }

    public void OnOrder(string id, int room, string summary, OrderStatus status)
    {
        lock (_gate)
        {
            if (status == OrderStatus.Delivered) _orders.Remove(id);
            else _orders[id] = new { id, room, summary, status = status.ToString() };
        }
    }

    /// <summary>A full, display-safe snapshot broadcast to every browser.</summary>
    public object Snapshot()
    {
        lock (_gate)
            return new
            {
                type = "snapshot",
                rooms = _rooms.Values.OrderBy(r => r.Number),
                orders = _orders.Values,
                issues = _issues.Values
            };
    }

    private RoomView Get(int number, string type, int floor)
    {
        if (!_rooms.TryGetValue(number, out var v))
            _rooms[number] = v = new RoomView { Number = number, Type = type, Floor = floor };
        return v;
    }
}
