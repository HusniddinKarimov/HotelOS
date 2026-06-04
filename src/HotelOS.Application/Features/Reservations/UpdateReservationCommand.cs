using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Reservations;

/// <summary>Updates the dates and preferences of a reservation that has not yet been checked in.</summary>
public record UpdateReservationCommand(
    Guid Id,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int? FloorPreference,
    string? ProximityPreference) : IRequest<ReservationDto>;

public class UpdateReservationCommandValidator : AbstractValidator<UpdateReservationCommand>
{
    public UpdateReservationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CheckInDate).LessThan(x => x.CheckOutDate)
            .WithMessage("Check-out must be after check-in.");
        RuleFor(x => x.ProximityPreference)
            .Must(p => p is null || p is "elevator" or "stairs")
            .WithMessage("Proximity preference must be 'elevator', 'stairs' or empty.");
    }
}

public class UpdateReservationCommandHandler : IRequestHandler<UpdateReservationCommand, ReservationDto>
{
    private readonly IUnitOfWork _uow;
    public UpdateReservationCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ReservationDto> Handle(UpdateReservationCommand request, CancellationToken ct)
    {
        var reservations = _uow.Repository<Reservation>();
        var reservation = await reservations.Query(tracking: true)
            .Include(r => r.Guest)
            .Include(r => r.RoomType)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException("Reservation", request.Id);

        if (reservation.Status is ReservationStatus.CheckedIn or ReservationStatus.CheckedOut or ReservationStatus.Cancelled)
            throw new ConflictException($"A {reservation.Status} reservation cannot be modified.");

        reservation.CheckInDate = request.CheckInDate;
        reservation.CheckOutDate = request.CheckOutDate;
        reservation.FloorPreference = request.FloorPreference;
        reservation.ProximityPreference = request.ProximityPreference;

        await _uow.SaveChangesAsync(ct);
        return reservation.ToDto();
    }
}
