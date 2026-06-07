using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using HotelOS.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.SelfService;

/// <summary>Find rooms available for a specific date range (the search step).</summary>
public record GetAvailableRoomsQuery(DateTime CheckIn, DateTime CheckOut) : IRequest<IReadOnlyList<AvailableRoomDto>>;

/// <summary>Returns the room the user is currently checked into, or null.</summary>
public record GetMyRoomQuery : IRequest<MyRoomDto?>;

public class GetAvailableRoomsQueryValidator : AbstractValidator<GetAvailableRoomsQuery>
{
    public GetAvailableRoomsQueryValidator()
    {
        RuleFor(x => x.CheckIn).LessThan(x => x.CheckOut).WithMessage("Check-out must be after check-in.");
        RuleFor(x => x.CheckIn).GreaterThanOrEqualTo(_ => DateTime.UtcNow.Date).WithMessage("Check-in cannot be in the past.");
        RuleFor(x => x).Must(x => (x.CheckOut.Date - x.CheckIn.Date).Days <= 30).WithMessage("Maximum stay is 30 nights.");
    }
}

public class GetAvailableRoomsQueryHandler : IRequestHandler<GetAvailableRoomsQuery, IReadOnlyList<AvailableRoomDto>>
{
    private readonly IUnitOfWork _uow;
    public GetAvailableRoomsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<AvailableRoomDto>> Handle(GetAvailableRoomsQuery request, CancellationToken ct)
    {
        // Load every room with its reservations so we can test each one for the range.
        var rooms = await _uow.Repository<Room>().Query()
            .Include(r => r.RoomType)
            .Include(r => r.Reservations)
            .ToListAsync(ct);

        var nights = Math.Max(1, (request.CheckOut.Date - request.CheckIn.Date).Days);

        return rooms
            .Where(r => AvailabilityService.IsRoomAvailable(r, r.Reservations, request.CheckIn, request.CheckOut))
            .OrderBy(r => r.Number)
            .Select(r => new AvailableRoomDto(
                r.Id, r.Number, r.Floor, r.RoomType.Name,
                r.RoomType.BaseRate, nights, r.RoomType.BaseRate * nights))
            .ToList();
    }
}

public class GetMyRoomQueryHandler : IRequestHandler<GetMyRoomQuery, MyRoomDto?>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetMyRoomQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<MyRoomDto?> Handle(GetMyRoomQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new AuthenticationException("Not signed in.");

        var reservation = await _uow.Repository<Reservation>().Query()
            .Include(r => r.Room)
            .Include(r => r.RoomType)
            .Include(r => r.Bill!).ThenInclude(b => b.Items)
            .FirstOrDefaultAsync(r => r.BookedByUserId == userId && r.Status == ReservationStatus.CheckedIn, ct);

        if (reservation?.Room is null) return null;

        return new MyRoomDto(
            reservation.Id, reservation.Room.Number, reservation.RoomType.Name, reservation.Room.Floor,
            reservation.Room.Status.ToString(), reservation.CheckInDate, reservation.CheckOutDate,
            reservation.Nights, reservation.Bill?.Id, reservation.Bill?.Total ?? 0m,
            reservation.Bill?.Status == BillStatus.Paid);
    }
}
