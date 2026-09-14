using FluentValidation;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Validators.Catalog;

public class MenuCategoryRequestValidator : AbstractValidator<MenuCategoryRequest>
{
    public MenuCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(100).WithMessage("Naziv može imati najviše 100 znakova.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Opis može imati najviše 1000 znakova.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Redoslijed prikaza ne može biti negativan broj.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Putanja do slike može imati najviše 500 znakova.");
    }
}
