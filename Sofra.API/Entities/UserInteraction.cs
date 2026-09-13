using Sofra.API.Enums;

namespace Sofra.API.Entities;

public class UserInteraction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int? MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    public InteractionType Type { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime CreatedAt { get; set; }
}
