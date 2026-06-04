using HotelOS.Domain.Entities;

namespace HotelOS.Application.Features.Rooms;

/// <summary>Display model for a room.</summary>
public record RoomDto(
    Guid Id,
    int Number,
    int Floor,
    string Type,
    int RoomTypeId,
    bool NearElevator,
    string Status,
    DateTime LastCleanedAt,
    string? CurrentGuest);

public static class RoomMapping
{
    public static RoomDto ToDto(this Room r) => new(
        r.Id, r.Number, r.Floor,
        r.RoomType?.Name ?? string.Empty, r.RoomTypeId,
        r.NearElevator, r.Status.ToString(), r.LastCleanedAt,
        r.CurrentGuest?.FullName);
}
