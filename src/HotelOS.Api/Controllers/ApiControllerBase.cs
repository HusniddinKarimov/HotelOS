using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Base for all API controllers; exposes the MediatR sender.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
