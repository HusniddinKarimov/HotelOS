using HotelOS.Domain.Entities;

namespace HotelOS.Application.Features.Maintenance;

public record MaintenanceDto(
    Guid Id,
    int RoomNumber,
    string Description,
    string Priority,
    string Status,
    long Sequence,
    Guid? AssignedToUserId,
    string? AssignedToName,
    DateTime ReportedAt,
    DateTime? ResolvedAt);

public static class MaintenanceMapping
{
    public static MaintenanceDto ToDto(this MaintenanceRequest m) => new(
        m.Id, m.RoomNumber, m.Description, m.Priority.ToString(), m.Status.ToString(),
        m.Sequence, m.AssignedToUserId, m.AssignedTo?.FullName, m.CreatedAt, m.ResolvedAt);
}
