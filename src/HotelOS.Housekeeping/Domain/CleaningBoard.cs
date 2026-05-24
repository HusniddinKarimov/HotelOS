namespace HotelOS.Housekeeping.Domain;

/// <summary>
/// Tracks rooms awaiting and undergoing cleaning. Uses a FIFO QUEUE so rooms
/// are cleaned in the order they were vacated (the data-structure choice
/// required by the brief), guarded by a lock for thread safety.
/// </summary>
public sealed class CleaningBoard
{
    private readonly Queue<int> _waiting = new();          // rooms reported dirty, FIFO
    private readonly HashSet<int> _known = new();          // de-dupe guard
    private readonly HashSet<int> _inProgress = new();     // rooms being cleaned now
    private readonly object _gate = new();

    /// <summary>Add a vacated room to the back of the cleaning queue.</summary>
    public bool Enqueue(int roomNumber)
    {
        lock (_gate)
        {
            if (!_known.Add(roomNumber)) return false; // already tracked
            _waiting.Enqueue(roomNumber);
            return true;
        }
    }

    /// <summary>Begin cleaning a specific room. Returns false if it isn't waiting.</summary>
    public bool StartCleaning(int roomNumber)
    {
        lock (_gate)
        {
            if (!_waiting.Contains(roomNumber)) return false;
            // Rebuild the queue without this room (small queue, cheap).
            var remaining = _waiting.Where(r => r != roomNumber).ToArray();
            _waiting.Clear();
            foreach (var r in remaining) _waiting.Enqueue(r);
            _inProgress.Add(roomNumber);
            return true;
        }
    }

    /// <summary>Finish cleaning a room. Returns false if it wasn't in progress.</summary>
    public bool FinishCleaning(int roomNumber)
    {
        lock (_gate)
        {
            if (!_inProgress.Remove(roomNumber)) return false;
            _known.Remove(roomNumber);
            return true;
        }
    }

    /// <summary>A snapshot for the API/dashboard.</summary>
    public object Snapshot()
    {
        lock (_gate)
            return new { waiting = _waiting.ToArray(), inProgress = _inProgress.ToArray() };
    }
}
