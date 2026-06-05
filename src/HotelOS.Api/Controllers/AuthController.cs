using HotelOS.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Authentication: login, token refresh and the current-user lookup.</summary>
public class AuthController : ApiControllerBase
{
    /// <summary>Public self-registration; creates a basic user and signs them in.</summary>
    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponse>> SignUp([FromBody] SignUpCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));

    /// <summary>Authenticate with username + password; returns access and refresh tokens.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));

    /// <summary>Exchange a refresh token for a new token pair.</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));

    /// <summary>Returns the identity of the authenticated caller.</summary>
    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> Me() => Ok(new
    {
        id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
        username = User.Identity?.Name,
        role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
    });
}
