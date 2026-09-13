using Sofra.API.Entities;

namespace Sofra.API.Services.Interfaces;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAtUtc, string Jti) CreateAccessToken(ApplicationUser user, IList<string> roles);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
