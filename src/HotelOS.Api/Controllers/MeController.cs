using HotelOS.Application.Features.Billing;
using HotelOS.Application.Features.Maintenance;
using HotelOS.Application.Features.Orders;
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

    /// <summary>Book a specific available room for a date range and pay by card.</summary>
    [HttpPost("book")]
    public async Task<ActionResult<MyRoomDto>> Book([FromBody] BookRoomCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));

    /// <summary>Leave the room — frees it to Dirty automatically.</summary>
    [HttpPost("leave")]
    public async Task<ActionResult<BillDto>> Leave(CancellationToken ct)
        => Ok(await Mediator.Send(new LeaveRoomCommand(), ct));

    // --- Room service --------------------------------------------------------

    /// <summary>The room-service menu.</summary>
    [HttpGet("menu")]
    public async Task<ActionResult<IReadOnlyList<MenuItem>>> Menu(CancellationToken ct)
        => Ok(await Mediator.Send(new GetMenuQuery(), ct));

    /// <summary>The current user's active room-service orders.</summary>
    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> MyOrders(CancellationToken ct)
        => Ok(await Mediator.Send(new GetMyOrdersQuery(), ct));

    /// <summary>Order room service for the user's own room.</summary>
    [HttpPost("orders")]
    public async Task<ActionResult<OrderDto>> PlaceOrder([FromBody] PlaceMyOrderCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));

    // --- Maintenance ---------------------------------------------------------

    /// <summary>Maintenance issues reported for the user's room.</summary>
    [HttpGet("issues")]
    public async Task<ActionResult<IReadOnlyList<MaintenanceDto>>> MyIssues(CancellationToken ct)
        => Ok(await Mediator.Send(new GetMyIssuesQuery(), ct));

    /// <summary>Report a maintenance issue for the user's own room.</summary>
    [HttpPost("issues")]
    public async Task<ActionResult<MaintenanceDto>> ReportIssue([FromBody] ReportMyIssueCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));
}
