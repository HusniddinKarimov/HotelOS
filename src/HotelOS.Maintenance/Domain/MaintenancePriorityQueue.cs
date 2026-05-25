using HotelOS.Contracts.Models;

namespace HotelOS.Maintenance.Domain;

/// <summary>
/// A binary min-heap priority queue for maintenance issues. Ordering rule:
///   1. Lower Urgency value first (Critical=0 beats Low=3).
///   2. On equal urgency, lower Sequence first (earliest submission wins).
/// Enqueue and Dequeue are O(log n). Implemented by hand so the algorithm is
/// explicit and owned, as the brief's priority-queue design requires.
/// </summary>
public sealed class MaintenancePriorityQueue
{
    private readonly List<MaintenanceIssue> _heap = new();

    public int Count => _heap.Count;

    /// <summary>Insert an issue and bubble it up to its correct position.</summary>
    public void Enqueue(MaintenanceIssue issue)
    {
        _heap.Add(issue);
        var i = _heap.Count - 1;
        while (i > 0)
        {
            var parent = (i - 1) / 2;
            if (Compare(_heap[i], _heap[parent]) >= 0) break;
            (_heap[i], _heap[parent]) = (_heap[parent], _heap[i]);
            i = parent;
        }
    }

    /// <summary>Remove and return the highest-priority issue, or null if empty.</summary>
    public MaintenanceIssue? Dequeue()
    {
        if (_heap.Count == 0) return null;
        var top = _heap[0];
        var last = _heap.Count - 1;
        _heap[0] = _heap[last];
        _heap.RemoveAt(last);

        // Sift the moved element down to restore the heap property.
        var i = 0;
        while (true)
        {
            int left = 2 * i + 1, right = 2 * i + 2, smallest = i;
            if (left < _heap.Count && Compare(_heap[left], _heap[smallest]) < 0) smallest = left;
            if (right < _heap.Count && Compare(_heap[right], _heap[smallest]) < 0) smallest = right;
            if (smallest == i) break;
            (_heap[i], _heap[smallest]) = (_heap[smallest], _heap[i]);
            i = smallest;
        }
        return top;
    }

    /// <summary>A peek of the queued issues in priority order (for the dashboard).</summary>
    public List<MaintenanceIssue> Snapshot() =>
        _heap.OrderBy(x => x, Comparer<MaintenanceIssue>.Create(Compare)).ToList();

    private static int Compare(MaintenanceIssue a, MaintenanceIssue b)
    {
        var byUrgency = ((int)a.Urgency).CompareTo((int)b.Urgency);
        return byUrgency != 0 ? byUrgency : a.Sequence.CompareTo(b.Sequence);
    }
}
