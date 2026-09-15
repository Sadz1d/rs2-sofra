using Sofra.API.DTOs.Payments;
using Sofra.API.Requests.Payments;

namespace Sofra.API.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentIntentResponse> CreateIntentAsync(PaymentIntentRequest request, int actorUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task HandleWebhookAsync(string json, string signature, CancellationToken cancellationToken = default);
}
