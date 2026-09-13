namespace Sofra.API.Entities;

public class Favorite
{
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
