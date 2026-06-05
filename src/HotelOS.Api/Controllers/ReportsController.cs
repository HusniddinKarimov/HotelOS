using HotelOS.Application.Features.Reports;
using HotelOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Management reports.</summary>
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.HotelManager}")]
public class ReportsController : ApiControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ReportsSummaryDto>> Summary(CancellationToken ct)
        => Ok(await Mediator.Send(new GetReportsSummaryQuery(), ct));
}
