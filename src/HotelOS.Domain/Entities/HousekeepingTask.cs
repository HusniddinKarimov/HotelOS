using HotelOS.Domain.Common;
using HotelOS.Domain.Enums;

namespace HotelOS.Domain.Entities;

/// <summary>A cleaning task queued when a room becomes Dirty.</summary>
public class HousekeepingTask : BaseEntity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int RoomNumber { get; set; }

    public HousekeepingStatus Status { get; set; } = HousekeepingStatus.Pending;

    public Guid? AssignedToUserId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
