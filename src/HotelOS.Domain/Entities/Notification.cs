using HotelOS.Domain.Common;
using HotelOS.Domain.Enums;

namespace HotelOS.Domain.Entities;

/// <summary>An in-app notification, optionally targeted at a role or user.</summary>
public class Notification : BaseEntity
{
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Target user (null = broadcast to a role or everyone).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Target role name (null = not role-scoped).</summary>
    public string? TargetRole { get; set; }

    public bool IsRead { get; set; }
}
