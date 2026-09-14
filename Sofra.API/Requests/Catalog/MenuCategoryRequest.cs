namespace Sofra.API.Requests.Catalog;

public class MenuCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
