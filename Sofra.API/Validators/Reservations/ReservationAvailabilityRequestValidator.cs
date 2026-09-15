using FluentValidation;
using Sofra.API.Requests.Reservations;

namespace Sofra.API.Validators.Reservations;

public class ReservationAvailabilityRequestValidator : AbstractValidator<ReservationAvailabilityRequest>
{
    public ReservationAvailabilityRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEqual(default(DateOnly)).WithMessage("Datum je obavezan.");

        RuleFor(x => x.Guests)
            .GreaterThan(0).WithMessage("Broj gostiju mora biti veći od 0.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(30, 480).WithMessage("Trajanje mora biti između 30 i 480 minuta.");
    }
}
