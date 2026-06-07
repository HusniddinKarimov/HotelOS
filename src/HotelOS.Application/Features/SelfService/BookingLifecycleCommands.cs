using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.SelfService;

/// <summary>Guest checks into one of their confirmed bookings (on/after arrival day).</summary>
public record CheckInBookingCommand(Guid ReservationId) : IRequest<MyRoomDto>;

/// <summary>Guest cancels a confirmed (not-yet-arrived) booking.</summary>
public record CancelBookingCommand(Guid ReservationId) : IRequest<BookingDto>;

public class CheckInBookingCommandHandler : IRequestHandler<CheckInBookingCommand, MyRoomDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IRealtimeNotifier _realtime;

    public CheckInBookingCommandHandler(IUnitOfWork uow, ICurrentUser currentUser, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _currentUser = currentUser;
        _realtime = realtime;
    }

    public async Task<MyRoomDto> Handle(CheckInBookingCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new AuthenticationException("Not signed in.");
        var reservations = _uow.Repository<Reservation>();

        var reservation = await reservations.Query(tracking: true)
            .Include(r => r.Room)
            .Include(r => r.RoomType)
            .Include(r => r.Bill!).ThenInclude(b => b.Items)
            .FirstOrDefaultAsync(r => r.Id == request.ReservationId && r.BookedByUserId == userId, ct)
            ?? throw new NotFoundException("Booking", request.ReservationId);

        if (reservation.Status != ReservationStatus.Confirmed)
            throw new ConflictException($"This booking is {reservation.Status} and cannot be checked in.");
        if (reservation.CheckInDate.Date > DateTime.UtcNow.Date)
            throw new ConflictException($"Check-in opens on {reservation.CheckInDate:dd MMM yyyy}.");

        // A guest can only be checked into one room at a time.
        if (await reservations.AnyAsync(r => r.BookedByUserId == userId && r.Status == ReservationStatus.CheckedIn, ct))
            throw new ConflictException("You are already checked into another room.");

        var room = reservation.Room ?? throw new ConflictException("No room is assigned to this booking.");
        room.Status = RoomStatus.Occupied;
        room.CurrentGuestId = reservation.GuestId;
        reservation.Status = ReservationStatus.CheckedIn;
        reservation.ActualCheckInAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);
        await _realtime.NotifyAsync(NotificationType.CheckIn, $"Guest checked into room {room.Number}.", ct: ct);
        await _realtime.ActivityAsync($"Check-in: room {room.Number} now occupied.", ct);

        return new MyRoomDto(reservation.Id, room.Number, reservation.RoomType.Name, room.Floor,
            room.Status.ToString(), reservation.CheckInDate, reservation.CheckOutDate, reservation.Nights,
            reservation.Bill?.Id, reservation.Bill?.Total ?? 0m, reservation.Bill?.Status == BillStatus.Paid);
    }
}

public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, BookingDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;

    public CancelBookingCommandHandler(IUnitOfWork uow, ICurrentUser currentUser, IAuditLogger audit)
    {
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<BookingDto> Handle(CancelBookingCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new AuthenticationException("Not signed in.");

        var reservation = await _uow.Repository<Reservation>().Query(tracking: true)
            .Include(r => r.Room)
            .Include(r => r.RoomType)
            .Include(r => r.Bill)
            .FirstOrDefaultAsync(r => r.Id == request.ReservationId && r.BookedByUserId == userId, ct)
            ?? throw new NotFoundException("Booking", request.ReservationId);

        if (reservation.Status != ReservationStatus.Confirmed)
            throw new ConflictException($"A {reservation.Status} booking cannot be cancelled here.");

        reservation.Status = ReservationStatus.Cancelled;
        if (reservation.Bill is { } bill) bill.Status = BillStatus.Cancelled; // prepaid amount refunded

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("CancelBooking", nameof(Reservation), reservation.Id.ToString(), reservation.ReferenceCode, ct);

        return new BookingDto(reservation.Id, reservation.ReferenceCode, reservation.Room?.Number, reservation.RoomType.Name,
            reservation.CheckInDate, reservation.CheckOutDate, reservation.Nights, reservation.Status.ToString(),
            reservation.Bill?.Total ?? 0m, false, false);
    }
}
