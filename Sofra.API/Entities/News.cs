namespace Sofra.API.Entities;

public class News
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime PublishAt { get; set; }
    public bool IsPublished { get; set; }
    public int CreatedById { get; set; }
    public ApplicationUser CreatedBy { get; set; } = null!;
    public bool SendPush { get; set; }
}
