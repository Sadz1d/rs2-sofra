using FluentValidation;
using Sofra.API.Constants;
using Sofra.API.Requests.Users;

namespace Sofra.API.Validators.Users;

public class CreateStaffUserRequestValidator : AbstractValidator<CreateStaffUserRequest>
{
    public CreateStaffUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Korisničko ime je obavezno.")
            .Length(3, 50).WithMessage("Korisničko ime mora imati između 3 i 50 znakova.")
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Korisničko ime smije sadržavati samo slova, brojeve, tačku, crticu i donju crtu.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail je obavezan.")
            .EmailAddress().WithMessage("E-mail mora biti u ispravnom formatu (npr. ime@domena.com).")
            .MaximumLength(256).WithMessage("E-mail može imati najviše 256 znakova.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Lozinka je obavezna.")
            .MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 znakova.")
            .Matches("[A-Z]").WithMessage("Lozinka mora sadržavati najmanje jedno veliko slovo.")
            .Matches("[a-z]").WithMessage("Lozinka mora sadržavati najmanje jedno malo slovo.")
            .Matches("[0-9]").WithMessage("Lozinka mora sadržavati najmanje jednu cifru.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ime je obavezno.")
            .MaximumLength(100).WithMessage("Ime može imati najviše 100 znakova.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Prezime je obavezno.")
            .MaximumLength(100).WithMessage("Prezime može imati najviše 100 znakova.");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9\s\-]{6,30}$").WithMessage("Broj telefona nije u ispravnom formatu.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Role)
            .Must(role => role == Roles.Admin || role == Roles.Konobar || role == Roles.Kuhar)
            .WithMessage("Uloga mora biti Admin, Konobar ili Kuhar.");

        RuleFor(x => x.Position)
            .NotEmpty().WithMessage("Pozicija je obavezna.")
            .MaximumLength(50).WithMessage("Pozicija može imati najviše 50 znakova.");
    }
}
