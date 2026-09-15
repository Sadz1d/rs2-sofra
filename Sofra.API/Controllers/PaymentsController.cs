using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sofra.API.Constants;
using Sofra.API.DTOs.Payments;
using Sofra.API.Extensions;
using Sofra.API.Requests.Payments;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController(IPaymentService service) : ControllerBase
{
    [HttpPost("intent")]
    public async Task<ActionResult<PaymentIntentResponse>> CreateIntent(PaymentIntentRequest request, CancellationToken cancellationToken)
    {
        var isStaff = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Konobar);
        var result = await service.CreateIntentAsync(request, User.GetUserId(), isStaff, cancellationToken);
        return Ok(result);
    }
}
