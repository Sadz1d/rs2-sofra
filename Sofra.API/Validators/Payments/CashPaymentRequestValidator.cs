using FluentValidation;
using Sofra.API.Requests.Payments;

namespace Sofra.API.Validators.Payments;

public class CashPaymentRequestValidator : AbstractValidator<CashPaymentRequest>
{
    public CashPaymentRequestValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("Narudžba je obavezna.");
    }
}
