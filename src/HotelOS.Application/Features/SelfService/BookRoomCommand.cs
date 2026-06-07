using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using HotelOS.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.SelfService;

/// <summary>
/// Books a room for a date range and pays by card up front (prepaid, like most
/// online travel sites). Creates a CONFIRMED reservation — the guest checks in
/// on arrival. The booking is rejected if the room is already reserved for any
/// overlapping dates. The whole thing runs in a Serializable transaction so two
/// simultaneous bookings can never both succeed for the same room and dates.
/// </summary>
public record BookRoomCommand(
    Guid RoomId, DateTime CheckIn, DateTime CheckOut, string FullName, string CardNumber)
    : IRequest<BookingDto>;

public class BookRoomCommandValidator : AbstractValidator<BookRoomCommand>
{
    public BookRoomCommandValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.CheckIn).LessThan(x => x.CheckOut).WithMessage("Check-out must be after check-in.");
        RuleFor(x => x.CheckIn).GreaterThanOrEqualTo(_ => DateTime.UtcNow.Date).WithMessage("Check-in cannot be in the past.");
        RuleFor(x => x).Must(x => (x.CheckOut.Date - x.CheckIn.Date).Days <= 30).WithMessage("Maximum stay is 30 nights.");
        RuleFor(x => x.CardNumber)
            .Must(c => System.Text.RegularExpressions.Regex.IsMatch(c?.Replace(" ", "") ?? "", @"^\d{12,19}$"))
            .WithMessage("Enter a valid card number (12–19 digits).");
    }
}

public class BookRoomCommandHandler : IRequestHandler<BookRoomCommand, BookingDto>
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

    public Task<BookingDto> Handle(BookRoomCommand request, CancellationToken ct) =>
        _uow.InSerializableTransactionAsync(() => BookAsync(request, ct), ct);

    private async Task<BookingDto> BookAsync(BookRoomCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new AuthenticationException("Not signed in.");

        // Load the room with its reservations and re-check availability for the dates.
        var room = await _uow.Repository<Room>().Query(tracking: true)
            .Include(r => r.RoomType)
            .Include(r => r.Reservations)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, ct)
            ?? throw new NotFoundException("Room", request.RoomId);

        if (!AvailabilityService.IsRoomAvailable(room, room.Reservations, request.CheckIn, request.CheckOut))
            throw new ConflictException($"Room {room.Number} is not available for those dates.");

        var user = await _uow.Repository<User>().GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        // Find-or-create the guest record for this account; use the name they entered.
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

        var nights = Math.Max(1, (request.CheckOut.Date - request.CheckIn.Date).Days);
        var total = room.RoomType.BaseRate * nights;

        var reservation = new Reservation
        {
            ReferenceCode = "RSV-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            GuestId = guest.Id,
            Guest = guest,
            RoomTypeId = room.RoomTypeId,
            RoomId = room.Id,
            CheckInDate = request.CheckIn,
            CheckOutDate = request.CheckOut,
            Status = ReservationStatus.Confirmed,   // booked & paid; not yet arrived
            BookedByUserId = userId,
        };

        // Prepaid: open the bill with the room charge and record the card payment.
        var bill = new Bill { ReservationId = reservation.Id, GuestId = guest.Id, Status = BillStatus.Paid };
        bill.Items.Add(new BillItem
        {
            BillId = bill.Id,
            Description = $"Room {room.Number} ({room.RoomType.Name}) — {nights} night(s) @ £{room.RoomType.BaseRate}",
            Type = BillItemType.Room,
            Amount = total,
            Quantity = nights,
        });
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

        await _uow.Repository<Reservation>().AddAsync(reservation, ct);
        await _uow.Repository<Bill>().AddAsync(bill, ct);
        await _uow.SaveChangesAsync(ct);

        await _realtime.NotifyAsync(NotificationType.PaymentCompleted,
            $"{guest.FullName} booked room {room.Number} ({request.CheckIn:dd MMM}–{request.CheckOut:dd MMM}, £{total:0.00}).", ct: ct);
        await _realtime.ActivityAsync($"New booking: room {room.Number}, {nights} night(s).", ct);

        return new BookingDto(reservation.Id, reservation.ReferenceCode, room.Number, room.RoomType.Name,
            reservation.CheckInDate, reservation.CheckOutDate, nights, reservation.Status.ToString(),
            total, Paid: true, CanCheckIn: request.CheckIn.Date <= DateTime.UtcNow.Date);
    }
}
