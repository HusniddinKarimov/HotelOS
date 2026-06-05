using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Maintenance;

/// <summary>
/// The maintenance priority queue: open/in-progress requests ranked by priority
/// (Critical first) then submission order (FIFO). Optionally includes completed.
/// </summary>
public class GetMaintenanceQuery : PagedQueryBase, IRequest<PagedResult<MaintenanceDto>>
{
    public bool IncludeCompleted { get; set; }
}

public class GetMaintenanceQueryHandler : IRequestHandler<GetMaintenanceQuery, PagedResult<MaintenanceDto>>
{
    private readonly IUnitOfWork _uow;
    public GetMaintenanceQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<MaintenanceDto>> Handle(GetMaintenanceQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<MaintenanceRequest>().Query()
            .Include(m => m.AssignedTo)
            .AsQueryable();

        if (!request.IncludeCompleted)
            query = query.Where(m => m.Status != MaintenanceStatus.Completed);

        // Priority queue ordering: Critical(0) first, then earliest submission.
        query = query.OrderBy(m => m.Priority).ThenBy(m => m.Sequence);

        var total = await query.CountAsync(ct);
        var list = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<MaintenanceDto>(list.Select(m => m.ToDto()).ToList(), request.Page, request.PageSize, total);
    }
}
