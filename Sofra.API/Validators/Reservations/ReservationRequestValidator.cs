using FluentValidation;
using Sofra.API.Requests.Reservations;

namespace Sofra.API.Validators.Reservations;

public class ReservationRequestValidator : AbstractValidator<ReservationRequest>
{
    public ReservationRequestValidator()
    {
        RuleFor(x => x.ReservationAt)
            .GreaterThan(_ => DateTime.UtcNow).WithMessage("Termin rezervacije mora biti u budućnosti.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(30, 480).WithMessage("Trajanje rezervacije mora biti između 30 i 480 minuta.");

        RuleFor(x => x.Guests)
            .GreaterThan(0).WithMessage("Broj gostiju mora biti veći od 0.");

        RuleFor(x => x.ZoneId)
            .GreaterThan(0).WithMessage("Zona je obavezna.");

        RuleFor(x => x.DiningTableId)
            .GreaterThan(0).WithMessage("Nevažeći sto.")
            .When(x => x.DiningTableId.HasValue);

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Napomena može imati najviše 500 znakova.");
    }
}
