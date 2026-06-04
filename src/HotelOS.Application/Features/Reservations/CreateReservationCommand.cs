using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Reservations;

/// <summary>Creates a confirmed reservation for a guest and a room type/date range.</summary>
public record CreateReservationCommand(
    Guid GuestId,
    int RoomTypeId,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int? FloorPreference,
    string? ProximityPreference) : IRequest<ReservationDto>;

public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.GuestId).NotEmpty();
        RuleFor(x => x.RoomTypeId).GreaterThan(0);
        RuleFor(x => x.CheckInDate).LessThan(x => x.CheckOutDate)
            .WithMessage("Check-out must be after check-in.");
        RuleFor(x => x.CheckOutDate)
            .Must((cmd, _) => (cmd.CheckOutDate.Date - cmd.CheckInDate.Date).Days <= 30)
            .WithMessage("Stays longer than 30 nights are not allowed.");
        RuleFor(x => x.ProximityPreference)
            .Must(p => p is null || p is "elevator" or "stairs")
            .WithMessage("Proximity preference must be 'elevator', 'stairs' or empty.");
    }
}

public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, ReservationDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogger _audit;

    public CreateReservationCommandHandler(IUnitOfWork uow, IAuditLogger audit)
    {
        _uow = uow;
        _audit = audit;
    }

    public async Task<ReservationDto> Handle(CreateReservationCommand request, CancellationToken ct)
    {
        var guests = _uow.Repository<Guest>();
        var types = _uow.Repository<RoomType>();
        var rooms = _uow.Repository<Room>();
        var reservations = _uow.Repository<Reservation>();

        var guest = await guests.GetByIdAsync(request.GuestId, ct)
            ?? throw new NotFoundException("Guest", request.GuestId);
        var type = await types.FirstOrDefaultAsync(t => t.Id == request.RoomTypeId, ct)
            ?? throw new NotFoundException("RoomType", request.RoomTypeId);

        // Overbooking guard: physical rooms of this type must exceed the count of
        // active reservations whose dates overlap the requested range.
        var capacity = await rooms.Query().CountAsync(r => r.RoomTypeId == type.Id, ct);
        var overlapping = await reservations.Query().CountAsync(r =>
            r.RoomTypeId == type.Id &&
            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn) &&
            r.CheckInDate < request.CheckOutDate && r.CheckOutDate > request.CheckInDate, ct);

        if (overlapping >= capacity)
            throw new ConflictException($"No {type.Name} rooms available for the requested dates.");

        var reservation = new Reservation
        {
            ReferenceCode = await GenerateUniqueCodeAsync(reservations, ct),
            GuestId = guest.Id,
            RoomTypeId = type.Id,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            FloorPreference = request.FloorPreference,
            ProximityPreference = request.ProximityPreference,
            Status = ReservationStatus.Confirmed
        };

        await reservations.AddAsync(reservation, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("CreateReservation", nameof(Reservation), reservation.Id.ToString(), reservation.ReferenceCode, ct);

        reservation.Guest = guest;
        reservation.RoomType = type;
        return reservation.ToDto();
    }

    private static async Task<string> GenerateUniqueCodeAsync(IGenericRepository<Reservation> repo, CancellationToken ct)
    {
        for (var i = 0; i < 10; i++)
        {
            var code = "RSV-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            if (!await repo.AnyAsync(r => r.ReferenceCode == code, ct))
                return code;
        }
        return "RSV-" + DateTime.UtcNow.Ticks.ToString("X");
    }
}
