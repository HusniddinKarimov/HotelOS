using HotelOS.Application.Common;
using HotelOS.Application.Features.Billing;
using HotelOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Bills / invoices.</summary>
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.HotelManager},{RoleNames.Receptionist}")]
[Route("api/bills")]
public class BillingController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<BillDto>>> Get([FromQuery] GetBillsQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BillDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetBillByIdQuery(id), ct));

    [HttpGet("by-reservation/{reservationId:guid}")]
    public async Task<ActionResult<BillDto>> GetByReservation(Guid reservationId, CancellationToken ct)
        => Ok(await Mediator.Send(new GetBillByReservationQuery(reservationId), ct));
}
