using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Application.Features.Billing;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Reservations;

/// <summary>
/// Checks a guest out: applies optional late-checkout fee and discount,
/// finalises the bill, frees the room (Dirty) and queues a cleaning task.
/// </summary>
public record CheckOutCommand(Guid ReservationId, bool LateCheckout = false, decimal DiscountPercent = 0m)
    : IRequest<BillDto>;

public class CheckOutCommandValidator : AbstractValidator<CheckOutCommand>
{
    public CheckOutCommandValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}

public class CheckOutCommandHandler : IRequestHandler<CheckOutCommand, BillDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;
    private readonly IAuditLogger _audit;

    public CheckOutCommandHandler(IUnitOfWork uow, IRealtimeNotifier realtime, IAuditLogger audit)
    {
        _uow = uow;
        _realtime = realtime;
        _audit = audit;
    }

    public async Task<BillDto> Handle(CheckOutCommand request, CancellationToken ct)
    {
        var reservation = await _uow.Repository<Reservation>().Query(tracking: true)
            .Include(r => r.Room)
            .Include(r => r.RoomType)
            .Include(r => r.Guest)
            .Include(r => r.Bill!).ThenInclude(b => b.Items)
            .FirstOrDefaultAsync(r => r.Id == request.ReservationId, ct)
            ?? throw new NotFoundException("Reservation", request.ReservationId);

        if (reservation.Status != ReservationStatus.CheckedIn)
            throw new ConflictException($"Reservation {reservation.ReferenceCode} is {reservation.Status}; only checked-in stays can be checked out.");

        var bill = reservation.Bill
            ?? throw new ConflictException("No bill exists for this reservation.");

        // New charges are added through the repository so EF tracks them as
        // INSERTs (adding to a loaded collection would be treated as UPDATEs
        // because the entity keys are client-generated).
        var billItems = _uow.Repository<BillItem>();
        var newItems = new List<BillItem>();

        // Late checkout fee = 50% of one night.
        if (request.LateCheckout)
            newItems.Add(new BillItem
            {
                BillId = bill.Id,
                Description = "Late checkout fee",
                Type = BillItemType.LateCheckout,
                Amount = Math.Round(reservation.RoomType.BaseRate * 0.5m, 2),
                Quantity = 1
            });

        // Discount applied on the running subtotal (stored as a positive Discount line).
        if (request.DiscountPercent > 0)
        {
            var subtotal = bill.Items.Where(i => i.Type != BillItemType.Discount).Sum(i => i.Amount)
                         + newItems.Where(i => i.Type != BillItemType.Discount).Sum(i => i.Amount);
            newItems.Add(new BillItem
            {
                BillId = bill.Id,
                Description = $"Discount ({request.DiscountPercent}%)",
                Type = BillItemType.Discount,
                Amount = Math.Round(subtotal * request.DiscountPercent / 100m, 2),
                Quantity = 1
            });
        }

        foreach (var item in newItems)
            await billItems.AddAsync(item, ct);

        // Free the room and queue it for cleaning.
        if (reservation.Room is { } room)
        {
            room.Status = RoomStatus.Dirty;
            room.CurrentGuestId = null;
            await _uow.Repository<HousekeepingTask>().AddAsync(new HousekeepingTask
            {
                RoomId = room.Id,
                RoomNumber = room.Number,
                Status = HousekeepingStatus.Pending
            }, ct);
        }

        reservation.Status = ReservationStatus.CheckedOut;
        reservation.ActualCheckOutAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("CheckOut", nameof(Reservation), reservation.Id.ToString(),
            $"Total £{bill.Total:0.00}", ct);
        await _realtime.NotifyAsync(NotificationType.CheckOut,
            $"{reservation.Guest.FullName} checked out. Total £{bill.Total:0.00}.", ct: ct);
        await _realtime.NotifyAsync(NotificationType.CleaningCompleted,
            $"Room {reservation.Room?.Number} added to the cleaning queue.", targetRole: RoleNames.Housekeeping, ct: ct);
        await _realtime.ActivityAsync($"Check-out complete; room {reservation.Room?.Number} is now Dirty.", ct);

        // Reload with all items (including the new charges) for an accurate invoice.
        var finalBill = await _uow.Repository<Bill>().Query()
            .Include(b => b.Items)
            .Include(b => b.Payments)
            .FirstAsync(b => b.Id == bill.Id, ct);
        return finalBill.ToDto();
    }
}
