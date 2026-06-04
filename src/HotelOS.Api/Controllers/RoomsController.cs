using HotelOS.Application.Common;
using HotelOS.Application.Features.Rooms;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Room inventory: list/filter/sort, fetch, create and status changes.</summary>
[Authorize]
public class RoomsController : ApiControllerBase
{
    /// <summary>Paged, filterable, sortable room list.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<RoomDto>>> Get([FromQuery] GetRoomsQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoomDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetRoomByIdQuery(id), ct));

    /// <summary>Create a room (Administrator or Hotel Manager).</summary>
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.HotelManager}")]
    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomCommand command, CancellationToken ct)
    {
        var room = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    /// <summary>Override a room's status (Administrator or Hotel Manager).</summary>
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.HotelManager}")]
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<RoomDto>> UpdateStatus(Guid id, [FromBody] UpdateRoomStatusRequest body, CancellationToken ct)
        => Ok(await Mediator.Send(new UpdateRoomStatusCommand(id, body.Status), ct));
}

/// <summary>Request body for a room status change.</summary>
public record UpdateRoomStatusRequest(RoomStatus Status);
