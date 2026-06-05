using HotelOS.Application.Features.Billing;
using HotelOS.Application.Features.SelfService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Guest self-service for the signed-in user: my room, book, leave.</summary>
[Authorize]
[Route("api/me")]
public class MeController : ApiControllerBase
{
    /// <summary>The room the current user occupies, or 204 if they have none.</summary>
    [HttpGet("room")]
    public async Task<ActionResult<MyRoomDto>> MyRoom(CancellationToken ct)
    {
        var room = await Mediator.Send(new GetMyRoomQuery(), ct);
        return room is null ? NoContent() : Ok(room);
    }

    /// <summary>Rooms the user can book right now.</summary>
    [HttpGet("available-rooms")]
    public async Task<ActionResult<IReadOnlyList<AvailableRoomDto>>> Available(CancellationToken ct)
        => Ok(await Mediator.Send(new GetAvailableRoomsQuery(), ct));

    /// <summary>Book a specific available room for the current user (checks them in).</summary>
    [HttpPost("book")]
    public async Task<ActionResult<MyRoomDto>> Book([FromBody] BookRoomBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new BookRoomCommand(body.RoomId), ct));

    /// <summary>Leave the room — frees it to Dirty automatically.</summary>
    [HttpPost("leave")]
    public async Task<ActionResult<BillDto>> Leave(CancellationToken ct)
        => Ok(await Mediator.Send(new LeaveRoomCommand(), ct));
}

public record BookRoomBody(Guid RoomId);
