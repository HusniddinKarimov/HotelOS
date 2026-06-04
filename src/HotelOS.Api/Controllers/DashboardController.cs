using HotelOS.Application.Features.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Aggregate metrics for the real-time operations dashboard.</summary>
[Authorize]
public class DashboardController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken ct)
        => Ok(await Mediator.Send(new GetDashboardQuery(), ct));
}
