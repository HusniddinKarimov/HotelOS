using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Rooms;

/// <summary>Paged, filterable, sortable list of rooms.</summary>
public class GetRoomsQuery : PagedQueryBase, IRequest<PagedResult<RoomDto>>
{
    public string? Status { get; set; }
    public int? RoomTypeId { get; set; }
    public int? Floor { get; set; }
}

public class GetRoomsQueryHandler : IRequestHandler<GetRoomsQuery, PagedResult<RoomDto>>
{
    private readonly IUnitOfWork _uow;
    public GetRoomsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<RoomDto>> Handle(GetRoomsQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<Room>().Query()
            .Include(r => r.RoomType)
            .Include(r => r.CurrentGuest)
            .AsQueryable();

        // Filtering
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<RoomStatus>(request.Status, true, out var status))
            query = query.Where(r => r.Status == status);
        if (request.RoomTypeId is int typeId)
            query = query.Where(r => r.RoomTypeId == typeId);
        if (request.Floor is int floor)
            query = query.Where(r => r.Floor == floor);

        // Searching (by room number)
        if (!string.IsNullOrWhiteSpace(request.Search) && int.TryParse(request.Search, out var num))
            query = query.Where(r => r.Number == num);

        // Sorting
        query = (request.SortBy?.ToLowerInvariant()) switch
        {
            "floor" => request.Descending ? query.OrderByDescending(r => r.Floor) : query.OrderBy(r => r.Floor),
            "status" => request.Descending ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "type" => request.Descending ? query.OrderByDescending(r => r.RoomType.Name) : query.OrderBy(r => r.RoomType.Name),
            _ => request.Descending ? query.OrderByDescending(r => r.Number) : query.OrderBy(r => r.Number)
        };

        var total = await query.CountAsync(ct);
        var rooms = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = rooms.Select(r => r.ToDto()).ToList();
        return new PagedResult<RoomDto>(items, request.Page, request.PageSize, total);
    }
}
