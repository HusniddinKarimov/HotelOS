using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.SelfService;

/// <summary>Returns the signed-in user's current room, or null if they have none.</summary>
public record GetMyRoomQuery : IRequest<MyRoomDto?>;

/// <summary>Lists rooms the user can book right now (status Clean).</summary>
public record GetAvailableRoomsQuery : IRequest<IReadOnlyList<AvailableRoomDto>>;

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
            reservation.Id,
            reservation.Room.Number,
            reservation.RoomType.Name,
            reservation.Room.Floor,
            reservation.Room.Status.ToString(),
            reservation.CheckInDate,
            reservation.CheckOutDate,
            reservation.Bill?.Id,
            reservation.Bill?.Total ?? 0m);
    }
}

public class GetAvailableRoomsQueryHandler : IRequestHandler<GetAvailableRoomsQuery, IReadOnlyList<AvailableRoomDto>>
{
    private readonly IUnitOfWork _uow;
    public GetAvailableRoomsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<AvailableRoomDto>> Handle(GetAvailableRoomsQuery request, CancellationToken ct)
    {
        var rooms = await _uow.Repository<Room>().Query()
            .Include(r => r.RoomType)
            .Where(r => r.Status == RoomStatus.Clean)
            .OrderBy(r => r.Number)
            .ToListAsync(ct);

        return rooms.Select(r => new AvailableRoomDto(r.Id, r.Number, r.Floor, r.RoomType.Name, r.RoomType.BaseRate)).ToList();
    }
}
