using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using HotelOS.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Reservations;

/// <summary>
/// Checks a guest in: runs the room-assignment algorithm, marks the room
/// Occupied, opens the bill with the room charge, and notifies the dashboard.
/// </summary>
public record CheckInCommand(Guid ReservationId) : IRequest<CheckInResultDto>;

public class CheckInCommandHandler : IRequestHandler<CheckInCommand, CheckInResultDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;
    private readonly IAuditLogger _audit;

    public CheckInCommandHandler(IUnitOfWork uow, IRealtimeNotifier realtime, IAuditLogger audit)
    {
        _uow = uow;
        _realtime = realtime;
        _audit = audit;
    }

    public async Task<CheckInResultDto> Handle(CheckInCommand request, CancellationToken ct)
    {
        var reservations = _uow.Repository<Reservation>();
        var rooms = _uow.Repository<Room>();

        var reservation = await reservations.Query(tracking: true)
            .Include(r => r.RoomType)
            .Include(r => r.Guest)
            .FirstOrDefaultAsync(r => r.Id == request.ReservationId, ct)
            ?? throw new NotFoundException("Reservation", request.ReservationId);

        if (reservation.Status is not (ReservationStatus.Confirmed or ReservationStatus.Pending))
            throw new ConflictException($"Reservation {reservation.ReferenceCode} is {reservation.Status} and cannot be checked in.");

        // Candidate rooms: matching type and currently Clean (tracked so we can update).
        var candidates = await rooms.Query(tracking: true)
            .Where(r => r.RoomTypeId == reservation.RoomTypeId && r.Status == RoomStatus.Clean)
            .ToListAsync(ct);

        var room = RoomAssignmentService.FindBestRoom(
            candidates, reservation.RoomTypeId, reservation.FloorPreference, reservation.ProximityPreference)
            ?? throw new ConflictException($"No clean {reservation.RoomType.Name} room is available for check-in.");

        // Occupy the room and link it to the reservation (prevents double-booking:
        // the room leaves the Clean pool immediately within this unit of work).
        room.Status = RoomStatus.Occupied;
        room.CurrentGuestId = reservation.GuestId;
        reservation.RoomId = room.Id;
        reservation.Status = ReservationStatus.CheckedIn;
        reservation.ActualCheckInAt = DateTime.UtcNow;

        // Open the bill with the room-night charge.
        var bill = new Bill
        {
            ReservationId = reservation.Id,
            GuestId = reservation.GuestId,
            Status = BillStatus.Open
        };
        bill.Items.Add(new BillItem
        {
            BillId = bill.Id,
            Description = $"Room {room.Number} ({reservation.RoomType.Name}) — {reservation.Nights} night(s)",
            Type = BillItemType.Room,
            Amount = reservation.RoomType.BaseRate * reservation.Nights,
            Quantity = reservation.Nights
        });
        await _uow.Repository<Bill>().AddAsync(bill, ct);

        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("CheckIn", nameof(Reservation), reservation.Id.ToString(),
            $"{reservation.Guest.FullName} -> room {room.Number}", ct);
        await _realtime.NotifyAsync(NotificationType.CheckIn,
            $"{reservation.Guest.FullName} checked in to room {room.Number}.", ct: ct);
        await _realtime.ActivityAsync($"Check-in: room {room.Number} now occupied.", ct);

        return new CheckInResultDto(reservation.Id, room.Number, room.Id, reservation.Status.ToString(), bill.Id);
    }
}
