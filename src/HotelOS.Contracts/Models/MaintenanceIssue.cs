namespace HotelOS.Contracts.Models;

/// <summary>
/// A reported maintenance fault. Ranked in the maintenance priority queue by
/// (<see cref="Urgency"/>, <see cref="Sequence"/>) so that equal-urgency
/// issues are served in submission order (FIFO tie-break).
/// </summary>
public class MaintenanceIssue
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..6];
    public int RoomNumber { get; init; }
    public string Description { get; init; } = string.Empty;
    public Urgency Urgency { get; init; }

    /// <summary>Monotonic submission counter used as the FIFO tie-breaker.</summary>
    public long Sequence { get; init; }

    public IssueStatus Status { get; set; } = IssueStatus.Open;
    public string? AssignedTechnician { get; set; }
    public DateTime ReportedUtc { get; init; } = DateTime.UtcNow;
}
