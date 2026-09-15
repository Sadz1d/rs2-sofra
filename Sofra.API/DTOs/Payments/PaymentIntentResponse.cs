namespace Sofra.API.DTOs.Payments;

public record PaymentIntentResponse(int PaymentId, string ClientSecret, decimal Amount, string Currency);
