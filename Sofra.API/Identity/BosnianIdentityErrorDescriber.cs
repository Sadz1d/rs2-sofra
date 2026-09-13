using Microsoft.AspNetCore.Identity;

namespace Sofra.API.Identity;

public class BosnianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = "Došlo je do nepoznate greške." };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"Korisničko ime '{userName}' je zauzeto." };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"E-mail '{email}' je već registrovan." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = "Korisničko ime sadrži nedozvoljene znakove." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "E-mail nije u ispravnom formatu." };

    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "Pogrešna lozinka." };

    public override IdentityError InvalidToken() =>
        new() { Code = nameof(InvalidToken), Description = "Kod nije validan ili je istekao." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"Lozinka mora imati najmanje {length} znakova." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Lozinka mora sadržavati najmanje jedan poseban znak." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "Lozinka mora sadržavati najmanje jednu cifru." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "Lozinka mora sadržavati najmanje jedno malo slovo." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "Lozinka mora sadržavati najmanje jedno veliko slovo." };
}
