namespace Sofra.API.Services.Interfaces;

public interface IJtiDenylistService
{
    void Deny(string jti, DateTime expiresAtUtc);
    bool IsDenied(string jti);
}
