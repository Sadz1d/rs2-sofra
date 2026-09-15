using FluentValidation;
using Sofra.API.Requests.Payments;

namespace Sofra.API.Validators.Payments;

public class PaymentIntentRequestValidator : AbstractValidator<PaymentIntentRequest>
{
    public PaymentIntentRequestValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("Narudžba je obavezna.");
    }
}
