using HotelOS.Domain.Common;
using HotelOS.Domain.Enums;

namespace HotelOS.Domain.Entities;

/// <summary>A reported maintenance fault ranked in the priority queue.</summary>
public class MaintenanceRequest : BaseEntity
{
    public int RoomNumber { get; set; }
    public string Description { get; set; } = string.Empty;

    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;

    /// <summary>Monotonic sequence for FIFO tie-breaking on equal priority.</summary>
    public long Sequence { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public User? AssignedTo { get; set; }

    public Guid? ReportedByUserId { get; set; }

    public DateTime? ResolvedAt { get; set; }
}
