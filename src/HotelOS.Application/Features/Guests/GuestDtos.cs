using HotelOS.Domain.Entities;

namespace HotelOS.Application.Features.Guests;

public record GuestDto(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string? Nationality,
    string? PassportNumber,
    DateTime CreatedAt);

public record GuestReservationSummary(
    Guid Id,
    string ReferenceCode,
    string RoomType,
    int? RoomNumber,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    string Status);

public record GuestDetailDto(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string? Nationality,
    string? PassportNumber,
    DateTime CreatedAt,
    IReadOnlyList<GuestReservationSummary> History);

public static class GuestMapping
{
    public static GuestDto ToDto(this Guest g) =>
        new(g.Id, g.FullName, g.Email, g.Phone, g.Nationality, g.PassportNumber, g.CreatedAt);
}
