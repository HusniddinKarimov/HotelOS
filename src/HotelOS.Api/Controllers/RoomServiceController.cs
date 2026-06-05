using HotelOS.Application.Common;
using HotelOS.Application.Features.Orders;
using HotelOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Room-service ordering and full order lifecycle.</summary>
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.HotelManager},{RoleNames.RoomServiceStaff},{RoleNames.Receptionist}")]
[Route("api/roomservice")]
public class RoomServiceController : ApiControllerBase
{
    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<OrderDto>>> Get([FromQuery] GetOrdersQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpPost("orders")]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));

    [HttpPost("orders/{id:guid}/advance")]
    public async Task<ActionResult<OrderDto>> Advance(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new AdvanceOrderCommand(id), ct));
}
