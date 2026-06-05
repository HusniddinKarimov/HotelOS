using HotelOS.Application.Features.Billing;
using HotelOS.Application.Features.Payments;
using HotelOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Api.Controllers;

/// <summary>Records payments against bills.</summary>
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.HotelManager},{RoleNames.Receptionist}")]
public class PaymentsController : ApiControllerBase
{
    /// <summary>Takes a payment and returns the updated bill.</summary>
    [HttpPost]
    public async Task<ActionResult<BillDto>> Pay([FromBody] CreatePaymentCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));
}
