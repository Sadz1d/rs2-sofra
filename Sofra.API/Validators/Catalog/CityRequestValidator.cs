using FluentValidation;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Validators.Catalog;

public class CityRequestValidator : AbstractValidator<CityRequest>
{
    public CityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(100).WithMessage("Naziv može imati najviše 100 znakova.");

        RuleFor(x => x.CountryId)
            .GreaterThan(0).WithMessage("Država je obavezna.");
    }
}
