using HotelOS.Application.Common;
using HotelOS.Application.Features.Auth;
using HotelOS.Application.Features.Users;
using HotelOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Staff user management (Administrator only).</summary>
[Authorize(Roles = RoleNames.Administrator)]
public class UsersController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> Get([FromQuery] GetUsersQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] RegisterUserCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));
}
