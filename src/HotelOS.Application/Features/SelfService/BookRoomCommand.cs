using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.SelfService;

/// <summary>
/// The signed-in user books a specific available room for a date range, paying
/// by card at the time of booking. Price = nights × nightly rate. The card is
/// never stored in full — only the last four digits are kept on the payment.
/// </summary>
public record BookRoomCommand(
    Guid RoomId,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    string FullName,
    string CardNumber) : IRequest<MyRoomDto>;

public class BookRoomCommandValidator : AbstractValidator<BookRoomCommand>
{
    public BookRoomCommandValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.CheckInDate).LessThan(x => x.CheckOutDate)
            .WithMessage("Check-out must be after check-in.");
        RuleFor(x => x.CheckOutDate)
            .Must((c, _) => (c.CheckOutDate.Date - c.CheckInDate.Date).Days <= 30)
            .WithMessage("Stays longer than 30 nights are not allowed.");
        RuleFor(x => x.CardNumber)
            .Must(card => System.Text.RegularExpressions.Regex.IsMatch(card?.Replace(" ", "") ?? "", @"^\d{12,19}$"))
            .WithMessage("Enter a valid card number (12–19 digits).");
    }
}

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

        var user = await _uow.Repository<User>().GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        // Find-or-create the guest record for this user; use the name they entered.
        var guests = _uow.Repository<Guest>();
        var guest = await guests.FirstOrDefaultAsync(g => g.Email == user.Email, ct);
        if (guest is null)
        {
            guest = new Guest { FullName = request.FullName.Trim(), Email = user.Email, Phone = "—" };
            await guests.AddAsync(guest, ct);
        }
        else
        {
            guest.FullName = request.FullName.Trim();
        }

        var nights = Math.Max(1, (request.CheckOutDate.Date - request.CheckInDate.Date).Days);
        var total = room.RoomType.BaseRate * nights;

        var reservation = new Reservation
        {
            ReferenceCode = "RSV-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            GuestId = guest.Id,
            Guest = guest,
            RoomTypeId = room.RoomTypeId,
            RoomId = room.Id,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Status = ReservationStatus.CheckedIn,
            ActualCheckInAt = DateTime.UtcNow,
            BookedByUserId = userId,
        };

        room.Status = RoomStatus.Occupied;
        room.CurrentGuestId = guest.Id;

        // Open the bill with the room charge for the whole stay.
        var bill = new Bill { ReservationId = reservation.Id, GuestId = guest.Id, Status = BillStatus.Paid };
        bill.Items.Add(new BillItem
        {
            BillId = bill.Id,
            Description = $"Room {room.Number} ({room.RoomType.Name}) — {nights} night(s) @ £{room.RoomType.BaseRate}",
            Type = BillItemType.Room,
            Amount = total,
            Quantity = nights,
        });
        // Record the card payment (masked — never store the full number).
        var digits = request.CardNumber.Replace(" ", "");
        bill.Payments.Add(new Payment
        {
            BillId = bill.Id,
            Method = PaymentMethod.Card,
            Status = PaymentStatus.Completed,
            Amount = total,
            Reference = $"Card ****{digits[^4..]}",
            PaidAt = DateTime.UtcNow,
        });
        reservation.Bill = bill;

        await reservations.AddAsync(reservation, ct);
        await _uow.Repository<Bill>().AddAsync(bill, ct);
        await _uow.SaveChangesAsync(ct);

        await _realtime.NotifyAsync(NotificationType.PaymentCompleted,
            $"{guest.FullName} booked room {room.Number} ({nights} night(s), £{total:0.00}).", ct: ct);
        await _realtime.ActivityAsync($"Room {room.Number} booked & paid — now occupied.", ct);

        return new MyRoomDto(reservation.Id, room.Number, room.RoomType.Name, room.Floor,
            room.Status.ToString(), reservation.CheckInDate, reservation.CheckOutDate, nights,
            bill.Id, bill.Total, Paid: true);
    }
}
