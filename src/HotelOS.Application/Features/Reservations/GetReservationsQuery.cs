using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Reservations;

/// <summary>Paged, filterable list of reservations.</summary>
public class GetReservationsQuery : PagedQueryBase, IRequest<PagedResult<ReservationDto>>
{
    public string? Status { get; set; }
    public Guid? GuestId { get; set; }
}

public class GetReservationsQueryHandler : IRequestHandler<GetReservationsQuery, PagedResult<ReservationDto>>
{
    private readonly IUnitOfWork _uow;
    public GetReservationsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<ReservationDto>> Handle(GetReservationsQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<Reservation>().Query()
            .Include(r => r.Guest)
            .Include(r => r.RoomType)
            .Include(r => r.Room)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ReservationStatus>(request.Status, true, out var status))
            query = query.Where(r => r.Status == status);
        if (request.GuestId is Guid gid)
            query = query.Where(r => r.GuestId == gid);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(r => r.ReferenceCode.ToLower().Contains(term) || r.Guest.FullName.ToLower().Contains(term));
        }

        query = request.Descending
            ? query.OrderByDescending(r => r.CheckInDate)
            : query.OrderBy(r => r.CheckInDate);

        var total = await query.CountAsync(ct);
        var list = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ReservationDto>(list.Select(r => r.ToDto()).ToList(), request.Page, request.PageSize, total);
    }
}
