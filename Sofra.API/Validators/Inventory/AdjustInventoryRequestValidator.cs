using FluentValidation;
using Sofra.API.Requests.Inventory;

namespace Sofra.API.Validators.Inventory;

public class AdjustInventoryRequestValidator : AbstractValidator<AdjustInventoryRequest>
{
    public AdjustInventoryRequestValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tip promjene stanja nije validan.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Količina mora biti veća od 0.");

        RuleFor(x => x.Note)
            .MaximumLength(300).WithMessage("Napomena može imati najviše 300 znakova.");
    }
}
