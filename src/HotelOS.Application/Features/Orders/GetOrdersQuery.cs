using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Orders;

/// <summary>Lists orders, optionally filtered by status; active orders first.</summary>
public class GetOrdersQuery : PagedQueryBase, IRequest<PagedResult<OrderDto>>
{
    public string? Status { get; set; }
    /// <summary>When true, returns only not-yet-delivered orders (kitchen/room-service view).</summary>
    public bool ActiveOnly { get; set; }
}

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    private readonly IUnitOfWork _uow;
    public GetOrdersQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<RoomServiceOrder>().Query().Include(o => o.Items).AsQueryable();

        if (request.ActiveOnly)
            query = query.Where(o => o.Status != OrderStatus.Delivered);
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<OrderStatus>(request.Status, true, out var st))
            query = query.Where(o => o.Status == st);
        if (!string.IsNullOrWhiteSpace(request.Search) && int.TryParse(request.Search, out var room))
            query = query.Where(o => o.RoomNumber == room);

        query = query.OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);
        var list = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<OrderDto>(list.Select(o => o.ToDto()).ToList(), request.Page, request.PageSize, total);
    }
}
