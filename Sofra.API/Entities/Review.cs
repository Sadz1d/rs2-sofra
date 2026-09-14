namespace Sofra.API.Entities;

public class Review
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int? MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    public int? WaiterId { get; set; }
    public ApplicationUser? Waiter { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? Reply { get; set; }
    public int? RepliedById { get; set; }
    public ApplicationUser? RepliedBy { get; set; }
    public DateTime? RepliedAt { get; set; }
}
