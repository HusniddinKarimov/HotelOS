using HotelOS.Application.Common;
using HotelOS.Application.Features.Orders;
using HotelOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Kitchen view: incoming orders and status advancement (Received→Preparing→Ready).</summary>
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.HotelManager},{RoleNames.KitchenStaff}")]
public class KitchenController : ApiControllerBase
{
    /// <summary>Active orders the kitchen needs to act on.</summary>
    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<OrderDto>>> Incoming([FromQuery] GetOrdersQuery query, CancellationToken ct)
    {
        query.ActiveOnly = true;
        return Ok(await Mediator.Send(query, ct));
    }

    [HttpPost("orders/{id:guid}/advance")]
    public async Task<ActionResult<OrderDto>> Advance(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new AdvanceOrderCommand(id), ct));
}
