using FluentValidation;
using Sofra.API.Requests.Payments;

namespace Sofra.API.Validators.Payments;

public class RefundRequestValidator : AbstractValidator<RefundRequest>
{
    public RefundRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Iznos povrata mora biti veći od 0.")
            .When(x => x.Amount.HasValue);
    }
}
