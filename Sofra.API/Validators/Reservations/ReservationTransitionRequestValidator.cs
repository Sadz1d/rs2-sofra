using FluentValidation;
using Sofra.API.Requests.Reservations;

namespace Sofra.API.Validators.Reservations;

public class ReservationTransitionRequestValidator : AbstractValidator<ReservationTransitionRequest>
{
    public ReservationTransitionRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status rezervacije nije validan.");

        RuleFor(x => x.RejectReason)
            .MaximumLength(500).WithMessage("Razlog odbijanja može imati najviše 500 znakova.");

        RuleFor(x => x.AlternativeAt)
            .GreaterThan(_ => DateTime.UtcNow).WithMessage("Predloženi alternativni termin mora biti u budućnosti.")
            .When(x => x.AlternativeAt.HasValue);
    }
}
