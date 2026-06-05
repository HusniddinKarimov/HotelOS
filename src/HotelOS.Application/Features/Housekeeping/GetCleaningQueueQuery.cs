using HotelOS.Application.Abstractions;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Housekeeping;

/// <summary>The cleaning queue: pending and in-progress tasks, oldest first.</summary>
public record GetCleaningQueueQuery : IRequest<IReadOnlyList<HousekeepingTaskDto>>;

public class GetCleaningQueueQueryHandler : IRequestHandler<GetCleaningQueueQuery, IReadOnlyList<HousekeepingTaskDto>>
{
    private readonly IUnitOfWork _uow;
    public GetCleaningQueueQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<HousekeepingTaskDto>> Handle(GetCleaningQueueQuery request, CancellationToken ct)
    {
        var tasks = await _uow.Repository<HousekeepingTask>().Query()
            .Where(t => t.Status != HousekeepingStatus.Completed)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

        return tasks.Select(t => t.ToDto()).ToList();
    }
}
