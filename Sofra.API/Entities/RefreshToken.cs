namespace Sofra.API.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByHash { get; set; }
    public DateTime CreatedAt { get; set; }
}
