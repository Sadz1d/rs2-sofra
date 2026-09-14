using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sofra.API.Constants;
using Sofra.API.Entities;
using Sofra.API.Options;

namespace Sofra.API.Data.Seed;

/// <summary>
/// Runtime seeder - pokrece se jednom pri startu API-ja (nakon migracije). Svaka grupa provjerava
/// sopstveno stanje prije upisa (idempotentno) da ponovno pokretanje kontejnera ne duplira podatke.
/// Korisnici se kreiraju iskljucivo preko UserManager-a, isti mehanizam kao i obicna registracija,
/// pa je hash lozinke uvijek u istom (Identity PBKDF2) formatu.
/// </summary>
public class DataSeeder(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<int>> roleManager,
    IOptions<SeedOptions> seedOptions,
    ILogger<DataSeeder> logger)
{
    private readonly string _defaultPassword = seedOptions.Value.DefaultPassword;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedUsersAsync(cancellationToken);

        // Sljedece grupe (sifarnici, jela, stolovi/namirnice, istorijski podaci) dodaju se ovdje.
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        var seedUsers = BuildSeedUsers();
        var createdCount = 0;

        foreach (var seedUser in seedUsers)
        {
            var existing = await userManager.FindByNameAsync(seedUser.Username);
            if (existing is not null)
            {
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = seedUser.Username,
                Email = seedUser.Email,
                FirstName = seedUser.FirstName,
                LastName = seedUser.LastName,
                Phone = seedUser.Phone,
                IsActive = true,
                EmailConfirmed = true,
            };

            // Test lozinka je namjerno "test" (dogovoreno u README-u, van pravila slozenosti za stvarnu
            // registraciju). CreateAsync(user, password) bi to odbio kroz PasswordValidator, pa hash
            // pravimo direktno preko istog IPasswordHasher-a koji Identity inace koristi - format ostaje
            // identican kao za korisnika koji se sam registruje, samo bez provjere slozenosti.
            user.PasswordHash = userManager.PasswordHasher.HashPassword(user, _defaultPassword);
            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                logger.LogError("Neuspjelo kreiranje seed korisnika {Username}: {Errors}",
                    seedUser.Username, string.Join("; ", createResult.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, seedUser.Role);
            createdCount++;

            if (seedUser.Position is not null)
            {
                var employee = new Employee
                {
                    UserId = user.Id,
                    Position = seedUser.Position,
                    HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-Random.Shared.Next(1, 4)).AddDays(-Random.Shared.Next(0, 365))),
                    Phone = seedUser.Phone,
                    IsActive = true,
                };
                dbContext.Employees.Add(employee);
                await dbContext.SaveChangesAsync(cancellationToken);

                AddCurrentWeekShifts(employee);
            }
        }

        if (createdCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seed korisnika: kreirano {Count} novih naloga.", createdCount);
        }
    }

    private void AddCurrentWeekShifts(Employee employee)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var isEveningShift = employee.Id % 2 == 0;
        var (startTime, endTime) = isEveningShift
            ? (new TimeOnly(16, 0), new TimeOnly(23, 0))
            : (new TimeOnly(8, 0), new TimeOnly(16, 0));

        for (var dayOffset = 0; dayOffset < 5; dayOffset++)
        {
            dbContext.Shifts.Add(new Shift
            {
                EmployeeId = employee.Id,
                Date = monday.AddDays(dayOffset),
                StartTime = startTime,
                EndTime = endTime,
            });
        }
    }

    private static List<SeedUser> BuildSeedUsers()
    {
        var users = new List<SeedUser>
        {
            new("admin", "admin@sofra.ba", "Adnan", "Hodžić", "+38761100001", Roles.Admin, "Menadžer"),
            new("desktop", "desktop@sofra.ba", "Amina", "Bešić", "+38761100002", Roles.Admin, "Menadžer"),
            new("konobar", "konobar@sofra.ba", "Emir", "Delić", "+38761100003", Roles.Konobar, "Konobar"),
            new("konobar2", "konobar2@sofra.ba", "Lejla", "Musić", "+38761100004", Roles.Konobar, "Konobar"),
            new("konobar3", "konobar3@sofra.ba", "Tarik", "Ahmetović", "+38761100005", Roles.Konobar, "Konobar"),
            new("kuhar", "kuhar@sofra.ba", "Selma", "Kovačević", "+38761100006", Roles.Kuhar, "Kuhar"),
            new("kuhar2", "kuhar2@sofra.ba", "Haris", "Zukić", "+38761100007", Roles.Kuhar, "Kuhar"),
            new("mobile", "mobile@sofra.ba", "Amar", "Softić", "+38761200000", Roles.Gost, null),
        };

        string[] gostFirstNames =
        [
            "Ivana", "Marko", "Ajla", "Faruk", "Dženana", "Sanjin", "Amela", "Denis",
            "Nađa", "Kenan", "Elma", "Adis", "Merima", "Vedad", "Alma",
        ];
        string[] gostLastNames =
        [
            "Hasanbegović", "Čolić", "Begić", "Musić", "Kadrić", "Salihović", "Hrustić", "Cengić",
            "Bajrić", "Kurtović", "Halilović", "Omerović", "Suljić", "Karić", "Hodžić",
        ];

        for (var i = 1; i <= 15; i++)
        {
            users.Add(new SeedUser(
                $"gost{i}",
                $"gost{i}@example.com",
                gostFirstNames[i - 1],
                gostLastNames[i - 1],
                $"+38761300{i:D3}",
                Roles.Gost,
                null));
        }

        return users;
    }

    private sealed record SeedUser(string Username, string Email, string FirstName, string LastName, string Phone, string Role, string? Position);
}
