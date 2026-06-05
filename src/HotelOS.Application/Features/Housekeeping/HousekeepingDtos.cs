using HotelOS.Domain.Entities;

namespace HotelOS.Application.Features.Housekeeping;

public record HousekeepingTaskDto(
    Guid Id,
    Guid RoomId,
    int RoomNumber,
    string Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt);

public static class HousekeepingMapping
{
    public static HousekeepingTaskDto ToDto(this HousekeepingTask t) =>
        new(t.Id, t.RoomId, t.RoomNumber, t.Status.ToString(), t.CreatedAt, t.StartedAt, t.CompletedAt);
}
