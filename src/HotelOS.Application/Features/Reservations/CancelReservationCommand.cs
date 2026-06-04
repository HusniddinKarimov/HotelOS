using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Reservations;

/// <summary>Cancels a reservation. If a room was held, it is released.</summary>
public record CancelReservationCommand(Guid Id) : IRequest<ReservationDto>;

public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, ReservationDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogger _audit;

    public CancelReservationCommandHandler(IUnitOfWork uow, IAuditLogger audit)
    {
        _uow = uow;
        _audit = audit;
    }

    public async Task<ReservationDto> Handle(CancelReservationCommand request, CancellationToken ct)
    {
        var reservation = await _uow.Repository<Reservation>().Query(tracking: true)
            .Include(r => r.Guest)
            .Include(r => r.RoomType)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException("Reservation", request.Id);

        if (reservation.Status is ReservationStatus.CheckedOut or ReservationStatus.Cancelled)
            throw new ConflictException($"A {reservation.Status} reservation cannot be cancelled.");

        // Release a held room (e.g. cancelling after check-in).
        if (reservation.Room is { } room && reservation.Status == ReservationStatus.CheckedIn)
        {
            room.Status = RoomStatus.Dirty;
            room.CurrentGuestId = null;
        }

        reservation.Status = ReservationStatus.Cancelled;
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("CancelReservation", nameof(Reservation), reservation.Id.ToString(), reservation.ReferenceCode, ct);

        return reservation.ToDto();
    }
}
