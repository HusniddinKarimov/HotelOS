using HotelOS.Application.Common;
using HotelOS.Application.Features.Guests;
using HotelOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Guest registration, search, details and history.</summary>
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.HotelManager},{RoleNames.Receptionist}")]
public class GuestsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<GuestDto>>> Get([FromQuery] GetGuestsQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GuestDetailDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetGuestByIdQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<GuestDto>> Create([FromBody] RegisterGuestCommand command, CancellationToken ct)
    {
        var guest = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = guest.Id }, guest);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GuestDto>> Update(Guid id, [FromBody] UpdateGuestBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new UpdateGuestCommand(id, body.FullName, body.Email, body.Phone, body.Nationality, body.PassportNumber), ct));

    /// <summary>Delete a guest. Administrator only; blocked if the guest has history.</summary>
    [Authorize(Roles = RoleNames.Administrator)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteGuestCommand(id), ct);
        return NoContent();
    }
}

public record UpdateGuestBody(string FullName, string Email, string Phone, string? Nationality, string? PassportNumber);
