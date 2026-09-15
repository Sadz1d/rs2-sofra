using FluentValidation;
using Sofra.API.Requests.Inventory;

namespace Sofra.API.Validators.Inventory;

public class InventoryItemRequestValidator : AbstractValidator<InventoryItemRequest>
{
    public InventoryItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(100).WithMessage("Naziv može imati najviše 100 znakova.");

        RuleFor(x => x.InventoryCategoryId)
            .GreaterThan(0).WithMessage("Kategorija namirnice je obavezna.");

        RuleFor(x => x.UnitOfMeasureId)
            .GreaterThan(0).WithMessage("Jedinica mjere je obavezna.");

        RuleFor(x => x.MinQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Minimalna količina ne može biti negativna.");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("Cijena po jedinici ne može biti negativna.")
            .When(x => x.UnitCost.HasValue);
    }
}
