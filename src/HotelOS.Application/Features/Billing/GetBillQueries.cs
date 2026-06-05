using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Billing;

/// <summary>Fetch a single bill (invoice) with its items and payments.</summary>
public record GetBillByIdQuery(Guid Id) : IRequest<BillDto>;

/// <summary>Fetch the bill for a reservation.</summary>
public record GetBillByReservationQuery(Guid ReservationId) : IRequest<BillDto>;

/// <summary>Paged list of bills, optionally filtered by status.</summary>
public class GetBillsQuery : PagedQueryBase, IRequest<PagedResult<BillDto>>
{
    public string? Status { get; set; }
}

public class GetBillByIdQueryHandler : IRequestHandler<GetBillByIdQuery, BillDto>
{
    private readonly IUnitOfWork _uow;
    public GetBillByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<BillDto> Handle(GetBillByIdQuery request, CancellationToken ct)
    {
        var bill = await _uow.Repository<Bill>().Query()
            .Include(b => b.Items).Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == request.Id, ct)
            ?? throw new NotFoundException("Bill", request.Id);
        return bill.ToDto();
    }
}

public class GetBillByReservationQueryHandler : IRequestHandler<GetBillByReservationQuery, BillDto>
{
    private readonly IUnitOfWork _uow;
    public GetBillByReservationQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<BillDto> Handle(GetBillByReservationQuery request, CancellationToken ct)
    {
        var bill = await _uow.Repository<Bill>().Query()
            .Include(b => b.Items).Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.ReservationId == request.ReservationId, ct)
            ?? throw new NotFoundException("Bill for reservation", request.ReservationId);
        return bill.ToDto();
    }
}

public class GetBillsQueryHandler : IRequestHandler<GetBillsQuery, PagedResult<BillDto>>
{
    private readonly IUnitOfWork _uow;
    public GetBillsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<BillDto>> Handle(GetBillsQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<Bill>().Query()
            .Include(b => b.Items).Include(b => b.Payments).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<BillStatus>(request.Status, true, out var st))
            query = query.Where(b => b.Status == st);

        query = query.OrderByDescending(b => b.CreatedAt);

        var total = await query.CountAsync(ct);
        var list = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<BillDto>(list.Select(b => b.ToDto()).ToList(), request.Page, request.PageSize, total);
    }
}
