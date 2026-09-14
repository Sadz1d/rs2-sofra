using FluentValidation;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Validators.Catalog;

public class PaymentMethodRequestValidator : AbstractValidator<PaymentMethodRequest>
{
    public PaymentMethodRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(50).WithMessage("Naziv može imati najviše 50 znakova.");

        RuleFor(x => x.Code)
            .MaximumLength(20).WithMessage("Šifra može imati najviše 20 znakova.");
    }
}
