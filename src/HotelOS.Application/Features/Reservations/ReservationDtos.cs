using HotelOS.Domain.Entities;

namespace HotelOS.Application.Features.Reservations;

public record ReservationDto(
    Guid Id,
    string ReferenceCode,
    Guid GuestId,
    string GuestName,
    int RoomTypeId,
    string RoomType,
    Guid? RoomId,
    int? RoomNumber,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int? FloorPreference,
    string? ProximityPreference,
    string Status,
    int Nights);

/// <summary>Result of a successful check-in.</summary>
public record CheckInResultDto(
    Guid ReservationId,
    int RoomNumber,
    Guid RoomId,
    string Status,
    Guid BillId);

public static class ReservationMapping
{
    public static ReservationDto ToDto(this Reservation r) => new(
        r.Id,
        r.ReferenceCode,
        r.GuestId,
        r.Guest?.FullName ?? string.Empty,
        r.RoomTypeId,
        r.RoomType?.Name ?? string.Empty,
        r.RoomId,
        r.Room?.Number,
        r.CheckInDate,
        r.CheckOutDate,
        r.FloorPreference,
        r.ProximityPreference,
        r.Status.ToString(),
        r.Nights);
}
