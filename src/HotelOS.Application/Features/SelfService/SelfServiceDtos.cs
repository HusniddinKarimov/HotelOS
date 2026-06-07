namespace HotelOS.Application.Features.SelfService;

/// <summary>A room offered for a specific date range, with the total already priced.</summary>
public record AvailableRoomDto(
    Guid RoomId, int Number, int Floor, string Type,
    decimal NightlyRate, int Nights, decimal Total);

/// <summary>One of the signed-in guest's bookings (past, current or upcoming).</summary>
public record BookingDto(
    Guid ReservationId,
    string ReferenceCode,
    int? RoomNumber,
    string RoomType,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int Nights,
    string Status,
    decimal Total,
    bool Paid,
    bool CanCheckIn);

/// <summary>The room the guest is currently checked into (their live stay).</summary>
public record MyRoomDto(
    Guid ReservationId,
    int RoomNumber,
    string RoomType,
    int Floor,
    string Status,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int Nights,
    Guid? BillId,
    decimal Total,
    bool Paid);
