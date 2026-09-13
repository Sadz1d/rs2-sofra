using Sofra.API.Enums;

namespace Sofra.API.Entities;

public class MenuItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int MenuCategoryId { get; set; }
    public MenuCategory MenuCategory { get; set; } = null!;
    public ServingPeriod ServingPeriods { get; set; }
    public bool IsAvailable { get; set; } = true;
    public decimal? AvgRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<MenuItemAllergen> MenuItemAllergens { get; set; } = new List<MenuItemAllergen>();
    public ICollection<MenuItemDietaryTag> MenuItemDietaryTags { get; set; } = new List<MenuItemDietaryTag>();
    public ICollection<MenuItemIngredient> Ingredients { get; set; } = new List<MenuItemIngredient>();
}
