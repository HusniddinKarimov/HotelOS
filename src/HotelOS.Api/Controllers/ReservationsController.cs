using HotelOS.Application.Common;
using HotelOS.Application.Features.Billing;
using HotelOS.Application.Features.Reservations;
using HotelOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Reservation lifecycle plus check-in and check-out.</summary>
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.HotelManager},{RoleNames.Receptionist}")]
public class ReservationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReservationDto>>> Get([FromQuery] GetReservationsQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReservationDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetReservationByIdQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create([FromBody] CreateReservationCommand command, CancellationToken ct)
    {
        var reservation = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, reservation);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReservationDto>> Update(Guid id, [FromBody] UpdateReservationBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new UpdateReservationCommand(id, body.CheckInDate, body.CheckOutDate, body.FloorPreference, body.ProximityPreference), ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ReservationDto>> Cancel(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new CancelReservationCommand(id), ct));

    /// <summary>Check the reservation's guest in (assigns the best available room).</summary>
    [HttpPost("{id:guid}/checkin")]
    public async Task<ActionResult<CheckInResultDto>> CheckIn(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new CheckInCommand(id), ct));

    /// <summary>Check the guest out and return the final bill.</summary>
    [HttpPost("{id:guid}/checkout")]
    public async Task<ActionResult<BillDto>> CheckOut(Guid id, [FromBody] CheckOutBody? body, CancellationToken ct)
        => Ok(await Mediator.Send(new CheckOutCommand(id, body?.LateCheckout ?? false, body?.DiscountPercent ?? 0m), ct));
}

public record UpdateReservationBody(DateTime CheckInDate, DateTime CheckOutDate, int? FloorPreference, string? ProximityPreference);
public record CheckOutBody(bool LateCheckout, decimal DiscountPercent);
