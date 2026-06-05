using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Orders;

public record NewOrderItem(string Name, int Quantity, decimal UnitPrice);

/// <summary>Places a room-service order and posts its cost to the guest's open bill.</summary>
public record CreateOrderCommand(int RoomNumber, IReadOnlyList<NewOrderItem> Items) : IRequest<OrderDto>;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.RoomNumber).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty().WithMessage("An order must contain at least one item.");
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            i.RuleFor(x => x.Quantity).GreaterThan(0);
            i.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;

    public CreateOrderCommandHandler(IUnitOfWork uow, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _realtime = realtime;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var orders = _uow.Repository<RoomServiceOrder>();

        var order = new RoomServiceOrder
        {
            OrderNumber = await GenerateNumberAsync(orders, ct),
            RoomNumber = request.RoomNumber,
            Status = OrderStatus.Received,
            Items = request.Items.Select(i => new RoomServiceOrderItem
            {
                Name = i.Name.Trim(),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        await orders.AddAsync(order, ct);

        // Auto-billing: if the room has a checked-in guest, post the charge to their bill.
        var reservation = await _uow.Repository<Reservation>().Query()
            .Include(r => r.Room)
            .Include(r => r.Bill)
            .FirstOrDefaultAsync(r => r.Status == ReservationStatus.CheckedIn
                                   && r.Room != null && r.Room.Number == request.RoomNumber, ct);

        if (reservation?.Bill is { } bill)
        {
            order.GuestId = reservation.GuestId;
            await _uow.Repository<BillItem>().AddAsync(new BillItem
            {
                BillId = bill.Id,
                Description = $"Room service {order.OrderNumber}",
                Type = BillItemType.Food,
                Amount = order.Items.Sum(i => i.Quantity * i.UnitPrice),
                Quantity = 1
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);

        await _realtime.NotifyAsync(NotificationType.NewOrder,
            $"New room-service order {order.OrderNumber} for room {order.RoomNumber}.",
            targetRole: RoleNames.KitchenStaff, ct: ct);
        await _realtime.ActivityAsync($"Order {order.OrderNumber} received for room {order.RoomNumber}.", ct);

        return order.ToDto();
    }

    private static async Task<string> GenerateNumberAsync(IGenericRepository<RoomServiceOrder> repo, CancellationToken ct)
    {
        for (var i = 0; i < 10; i++)
        {
            var n = "RS-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            if (!await repo.AnyAsync(o => o.OrderNumber == n, ct)) return n;
        }
        return "RS-" + DateTime.UtcNow.Ticks.ToString("X");
    }
}
