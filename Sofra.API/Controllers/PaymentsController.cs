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

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await service.HandleWebhookAsync(json, signature, cancellationToken);
        return Ok();
    }

    [HttpPost("cash")]
    [Authorize(Roles = $"{Roles.Konobar},{Roles.Admin}")]
    public async Task<ActionResult<PaymentResponse>> CreateCash(CashPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateCashPaymentAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/refund")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PaymentResponse>> Refund(int id, RefundRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RefundAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
