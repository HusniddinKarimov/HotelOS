using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.SelfService;

/// <summary>
/// The signed-in user books a specific available room for themselves (1 night),
/// which immediately checks them in. Fails if they already hold a room or the
/// room is no longer Clean.
/// </summary>
public record BookRoomCommand(Guid RoomId) : IRequest<MyRoomDto>;

public class BookRoomCommandHandler : IRequestHandler<BookRoomCommand, MyRoomDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IRealtimeNotifier _realtime;

    public BookRoomCommandHandler(IUnitOfWork uow, ICurrentUser currentUser, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _currentUser = currentUser;
        _realtime = realtime;
    }

    public async Task<MyRoomDto> Handle(BookRoomCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new AuthenticationException("Not signed in.");
        var reservations = _uow.Repository<Reservation>();

        // One active room per user.
        if (await reservations.AnyAsync(r => r.BookedByUserId == userId && r.Status == ReservationStatus.CheckedIn, ct))
            throw new ConflictException("You already have a room. Leave it before booking another.");

        var room = await _uow.Repository<Room>().Query(tracking: true)
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, ct)
            ?? throw new NotFoundException("Room", request.RoomId);

        if (room.Status != RoomStatus.Clean)
            throw new ConflictException($"Room {room.Number} is no longer available.");

        // Identify the user so we can attach a guest record.
        var user = await _uow.Repository<User>().GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        // Find-or-create the guest record for this user (matched by email).
        var guests = _uow.Repository<Guest>();
        var guest = await guests.FirstOrDefaultAsync(g => g.Email == user.Email, ct);
        if (guest is null)
        {
            guest = new Guest { FullName = user.FullName, Email = user.Email, Phone = "—" };
            await guests.AddAsync(guest, ct);
        }

        var now = DateTime.UtcNow;
        var reservation = new Reservation
        {
            ReferenceCode = "RSV-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            GuestId = guest.Id,
            Guest = guest,
            RoomTypeId = room.RoomTypeId,
            RoomId = room.Id,
            CheckInDate = now,
            CheckOutDate = now.AddDays(1),
            Status = ReservationStatus.CheckedIn,
            ActualCheckInAt = now,
            BookedByUserId = userId,
        };

        // Occupy the room and open the bill.
        room.Status = RoomStatus.Occupied;
        room.CurrentGuestId = guest.Id;

        var bill = new Bill { ReservationId = reservation.Id, GuestId = guest.Id, Status = BillStatus.Open };
        bill.Items.Add(new BillItem
        {
            BillId = bill.Id,
            Description = $"Room {room.Number} ({room.RoomType.Name}) — 1 night",
            Type = BillItemType.Room,
            Amount = room.RoomType.BaseRate,
            Quantity = 1,
        });
        reservation.Bill = bill;

        await reservations.AddAsync(reservation, ct);
        await _uow.Repository<Bill>().AddAsync(bill, ct);
        await _uow.SaveChangesAsync(ct);

        await _realtime.NotifyAsync(NotificationType.CheckIn, $"{user.FullName} booked room {room.Number}.", ct: ct);
        await _realtime.ActivityAsync($"Room {room.Number} self-booked and occupied.", ct);

        return new MyRoomDto(reservation.Id, room.Number, room.RoomType.Name, room.Floor,
            room.Status.ToString(), reservation.CheckInDate, reservation.CheckOutDate, bill.Id, bill.Total);
    }
}
