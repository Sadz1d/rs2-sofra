using FluentValidation;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Validators.Catalog;

public class CountryRequestValidator : AbstractValidator<CountryRequest>
{
    public CountryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(100).WithMessage("Naziv može imati najviše 100 znakova.");

        RuleFor(x => x.Code)
            .MaximumLength(5).WithMessage("Šifra može imati najviše 5 znakova.");
    }
}
