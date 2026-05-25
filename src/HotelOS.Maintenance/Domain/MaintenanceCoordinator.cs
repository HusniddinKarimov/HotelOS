using HotelOS.Contracts.Models;

namespace HotelOS.Maintenance.Domain;

/// <summary>
/// Coordinates the maintenance priority queue and the pool of technicians.
/// When an issue is reported it is queued by priority and the coordinator
/// immediately tries to dispatch waiting issues to free technicians. When an
/// issue is resolved the technician is freed and the next-highest issue is
/// dispatched. All mutations are serialised by a lock.
/// </summary>
public sealed class MaintenanceCoordinator
{
    private readonly MaintenancePriorityQueue _queue = new();
    private readonly Dictionary<string, MaintenanceIssue> _all = new();
    private readonly List<Technician> _technicians;
    private readonly object _gate = new();
    private long _sequence;

    public MaintenanceCoordinator()
    {
        _technicians = new List<Technician>
        {
            new("T1", "Alex Carter"),
            new("T2", "Priya Shah")
        };
    }

    /// <summary>
    /// Report a fault. Returns the changed issues (the new one, plus any that
    /// just got dispatched) so the caller can publish broker events.
    /// </summary>
    public List<MaintenanceIssue> Report(int roomNumber, string description, Urgency urgency)
    {
        lock (_gate)
        {
            var issue = new MaintenanceIssue
            {
                RoomNumber = roomNumber,
                Description = description,
                Urgency = urgency,
                Sequence = _sequence++
            };
            _all[issue.Id] = issue;
            _queue.Enqueue(issue);
            return DispatchAll(includeFirst: issue);
        }
    }

    /// <summary>Mark an issue resolved, free its technician, dispatch the next.</summary>
    public List<MaintenanceIssue>? Resolve(string issueId)
    {
        lock (_gate)
        {
            if (!_all.TryGetValue(issueId, out var issue) || issue.Status == IssueStatus.Resolved)
                return null;

            issue.Status = IssueStatus.Resolved;
            // AssignedTechnician holds the technician's NAME (see DispatchAll),
            // so the pool must be matched by Name — not Id — to free them.
            var tech = _technicians.FirstOrDefault(t => t.Name == issue.AssignedTechnician);
            if (tech is not null) tech.IsAvailable = true;

            var changed = DispatchAll();
            changed.Insert(0, issue);
            return changed;
        }
    }

    /// <summary>Assign queued issues to free technicians, highest priority first.</summary>
    private List<MaintenanceIssue> DispatchAll(MaintenanceIssue? includeFirst = null)
    {
        var changed = new List<MaintenanceIssue>();
        if (includeFirst is not null) changed.Add(includeFirst);

        while (_queue.Count > 0)
        {
            var tech = _technicians.FirstOrDefault(t => t.IsAvailable);
            if (tech is null) break; // no free technician -> leave issues queued

            var next = _queue.Dequeue()!;
            tech.IsAvailable = false;
            next.Status = IssueStatus.Assigned;
            next.AssignedTechnician = tech.Name;
            if (!changed.Contains(next)) changed.Add(next);
        }
        return changed;
    }

    public object Snapshot()
    {
        lock (_gate)
            return new
            {
                queued = _queue.Snapshot().Select(Project),
                assigned = _all.Values.Where(i => i.Status == IssueStatus.Assigned).Select(Project),
                technicians = _technicians.Select(t => new { t.Name, t.IsAvailable, role = t.Describe() })
            };
    }

    private static object Project(MaintenanceIssue i) => new
    {
        i.Id, i.RoomNumber, i.Description,
        urgency = i.Urgency.ToString(),
        status = i.Status.ToString(),
        technician = i.AssignedTechnician
    };
}
