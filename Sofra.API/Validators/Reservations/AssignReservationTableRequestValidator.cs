using FluentValidation;
using Sofra.API.Requests.Reservations;

namespace Sofra.API.Validators.Reservations;

public class AssignReservationTableRequestValidator : AbstractValidator<AssignReservationTableRequest>
{
    public AssignReservationTableRequestValidator()
    {
        RuleFor(x => x.DiningTableId)
            .GreaterThan(0).WithMessage("Sto je obavezan.");
    }
}
