using HotelOS.Domain.Common;

namespace HotelOS.Domain.Entities;

/// <summary>An immutable record of a security-relevant action.</summary>
public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Entity { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}
