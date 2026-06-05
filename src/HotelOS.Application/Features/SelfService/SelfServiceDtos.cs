namespace HotelOS.Application.Features.SelfService;

/// <summary>The room the signed-in user currently occupies (their own stay).</summary>
public record MyRoomDto(
    Guid ReservationId,
    int RoomNumber,
    string RoomType,
    int Floor,
    string Status,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    Guid? BillId,
    decimal Total);

/// <summary>A room the user is allowed to book (currently Clean).</summary>
public record AvailableRoomDto(Guid RoomId, int Number, int Floor, string Type, decimal NightlyRate);
