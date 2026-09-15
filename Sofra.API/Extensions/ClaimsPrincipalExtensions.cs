using System.Security.Claims;

namespace Sofra.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Korisnik nije autentifikovan.");
        return int.Parse(value);
    }

    public static IReadOnlyCollection<string> GetRoles(this ClaimsPrincipal principal) =>
        principal.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();
}
