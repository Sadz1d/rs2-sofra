using Sofra.API.Enums;

namespace Sofra.API.Requests.Menu;

public class MenuItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int MenuCategoryId { get; set; }
    public ServingPeriod ServingPeriods { get; set; }
    public bool IsAvailable { get; set; } = true;
    public List<int> AllergenIds { get; set; } = [];
    public List<int> DietaryTagIds { get; set; } = [];
}
