using FluentValidation;
using Sofra.API.Enums;
using Sofra.API.Requests.Promotions;

namespace Sofra.API.Validators.Promotions;

public class PromotionRequestValidator : AbstractValidator<PromotionRequest>
{
    public PromotionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(100).WithMessage("Naziv može imati najviše 100 znakova.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Kod je obavezan.")
            .MaximumLength(20).WithMessage("Kod može imati najviše 20 znakova.")
            .Matches("^[A-Za-z0-9]+$").WithMessage("Kod smije sadržavati samo slova i brojeve.");

        RuleFor(x => x.DiscountType).IsInEnum().WithMessage("Tip popusta nije validan.");
        RuleFor(x => x.Scope).IsInEnum().WithMessage("Opseg promocije nije validan.");

        RuleFor(x => x.Value)
            .InclusiveBetween(1, 100).WithMessage("Postotak popusta mora biti između 1 i 100.")
            .When(x => x.DiscountType == DiscountType.Percentage);

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Iznos popusta mora biti veći od 0.")
            .When(x => x.DiscountType == DiscountType.FixedAmount);

        RuleFor(x => x.MenuCategoryId)
            .NotNull().WithMessage("Kategorija je obavezna kad je opseg promocije 'Category'.")
            .GreaterThan(0).WithMessage("Kategorija je obavezna kad je opseg promocije 'Category'.")
            .When(x => x.Scope == PromotionScope.Category);

        RuleFor(x => x.ValidTo)
            .GreaterThan(x => x.ValidFrom).WithMessage("Datum isteka mora biti poslije datuma početka važenja.");

        RuleFor(x => x.MaxUses)
            .GreaterThan(0).WithMessage("Maksimalan broj upotreba mora biti veći od 0.")
            .When(x => x.MaxUses.HasValue);

        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Minimalan iznos narudžbe ne može biti negativan.")
            .When(x => x.MinOrderAmount.HasValue);
    }
}
