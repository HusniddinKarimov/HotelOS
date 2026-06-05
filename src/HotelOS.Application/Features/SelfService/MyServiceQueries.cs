using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Application.Features.Maintenance;
using HotelOS.Application.Features.Orders;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.SelfService;

/// <summary>The room-service menu (name + price) for the booking screens.</summary>
public record GetMenuQuery : IRequest<IReadOnlyList<MenuItem>>;

/// <summary>The current user's room-service orders (active ones).</summary>
public record GetMyOrdersQuery : IRequest<IReadOnlyList<OrderDto>>;

/// <summary>The current user's reported maintenance issues.</summary>
public record GetMyIssuesQuery : IRequest<IReadOnlyList<MaintenanceDto>>;

public class GetMenuQueryHandler : IRequestHandler<GetMenuQuery, IReadOnlyList<MenuItem>>
{
    public Task<IReadOnlyList<MenuItem>> Handle(GetMenuQuery request, CancellationToken ct) =>
        Task.FromResult(Menu.Items);
}

/// <summary>Shared helper: the room number of the user's active stay (or null).</summary>
internal static class SelfServiceHelpers
{
    public static async Task<int?> CurrentRoomNumberAsync(IUnitOfWork uow, Guid userId, CancellationToken ct)
    {
        var reservation = await uow.Repository<Reservation>().Query()
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.BookedByUserId == userId && r.Status == ReservationStatus.CheckedIn, ct);
        return reservation?.Room?.Number;
    }
}

public class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, IReadOnlyList<OrderDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetMyOrdersQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<OrderDto>> Handle(GetMyOrdersQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new AuthenticationException("Not signed in.");
        var roomNumber = await SelfServiceHelpers.CurrentRoomNumberAsync(_uow, userId, ct);
        if (roomNumber is null) return Array.Empty<OrderDto>();

        var orders = await _uow.Repository<RoomServiceOrder>().Query()
            .Include(o => o.Items)
            .Where(o => o.RoomNumber == roomNumber && o.Status != OrderStatus.Delivered)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return orders.Select(o => o.ToDto()).ToList();
    }
}

public class GetMyIssuesQueryHandler : IRequestHandler<GetMyIssuesQuery, IReadOnlyList<MaintenanceDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetMyIssuesQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MaintenanceDto>> Handle(GetMyIssuesQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new AuthenticationException("Not signed in.");
        var roomNumber = await SelfServiceHelpers.CurrentRoomNumberAsync(_uow, userId, ct);
        if (roomNumber is null) return Array.Empty<MaintenanceDto>();

        var issues = await _uow.Repository<MaintenanceRequest>().Query()
            .Include(m => m.AssignedTo)
            .Where(m => m.RoomNumber == roomNumber)
            .OrderByDescending(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        return issues.Select(m => m.ToDto()).ToList();
    }
}
