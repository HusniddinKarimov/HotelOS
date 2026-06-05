using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Orders;

/// <summary>Advances an order to its next state (Received→Preparing→Ready→Delivering→Delivered).</summary>
public record AdvanceOrderCommand(Guid OrderId) : IRequest<OrderDto>;

public class AdvanceOrderCommandHandler : IRequestHandler<AdvanceOrderCommand, OrderDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;

    public AdvanceOrderCommandHandler(IUnitOfWork uow, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _realtime = realtime;
    }

    public async Task<OrderDto> Handle(AdvanceOrderCommand request, CancellationToken ct)
    {
        var order = await _uow.Repository<RoomServiceOrder>().Query(tracking: true)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new NotFoundException("Order", request.OrderId);

        order.Status = order.Status switch
        {
            OrderStatus.Received => OrderStatus.Preparing,
            OrderStatus.Preparing => OrderStatus.Ready,
            OrderStatus.Ready => OrderStatus.Delivering,
            OrderStatus.Delivering => OrderStatus.Delivered,
            _ => throw new ConflictException("Order is already delivered.")
        };

        await _uow.SaveChangesAsync(ct);
        await _realtime.ActivityAsync($"Order {order.OrderNumber} is now {order.Status}.", ct);

        return order.ToDto();
    }
}
