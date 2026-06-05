using HotelOS.Application.Features.Housekeeping;
using HotelOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Cleaning queue and room-status workflow.</summary>
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.HotelManager},{RoleNames.Housekeeping}")]
public class HousekeepingController : ApiControllerBase
{
    [HttpGet("queue")]
    public async Task<ActionResult<IReadOnlyList<HousekeepingTaskDto>>> Queue(CancellationToken ct)
        => Ok(await Mediator.Send(new GetCleaningQueueQuery(), ct));

    [HttpPost("{taskId:guid}/start")]
    public async Task<ActionResult<HousekeepingTaskDto>> Start(Guid taskId, CancellationToken ct)
        => Ok(await Mediator.Send(new StartCleaningCommand(taskId), ct));

    [HttpPost("{taskId:guid}/complete")]
    public async Task<ActionResult<HousekeepingTaskDto>> Complete(Guid taskId, CancellationToken ct)
        => Ok(await Mediator.Send(new CompleteCleaningCommand(taskId), ct));
}
