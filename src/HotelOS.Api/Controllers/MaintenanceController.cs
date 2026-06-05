using HotelOS.Application.Common;
using HotelOS.Application.Features.Maintenance;
using HotelOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Maintenance requests: priority queue, assignment and resolution.</summary>
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.HotelManager},{RoleNames.MaintenanceStaff},{RoleNames.Receptionist}")]
public class MaintenanceController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<MaintenanceDto>>> Get([FromQuery] GetMaintenanceQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpPost]
    public async Task<ActionResult<MaintenanceDto>> Create([FromBody] CreateMaintenanceRequestCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<MaintenanceDto>> Assign(Guid id, [FromBody] AssignBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new AssignMaintenanceCommand(id, body.TechnicianUserId), ct));

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<MaintenanceDto>> Resolve(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new ResolveMaintenanceCommand(id), ct));
}

public record AssignBody(Guid TechnicianUserId);
