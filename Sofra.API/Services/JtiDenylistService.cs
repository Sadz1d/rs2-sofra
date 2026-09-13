using Microsoft.Extensions.Caching.Memory;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

/// <summary>
/// Opoziva JWT access tokene prije njihovog prirodnog isteka (logout).
/// Kes drzi jti samo do isteka access tokena (max 1440 min po JwtOptions), pa je IMemoryCache dovoljan
/// za jednu instancu API-ja - nema potrebe za posebnom tabelom u bazi.
/// </summary>
public class JtiDenylistService(IMemoryCache cache) : IJtiDenylistService
{
    private const string KeyPrefix = "jti-denylist:";

    public void Deny(string jti, DateTime expiresAtUtc) =>
        cache.Set(KeyPrefix + jti, true, expiresAtUtc);

    public bool IsDenied(string jti) => cache.TryGetValue(KeyPrefix + jti, out _);
}
