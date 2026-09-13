namespace Sofra.API.Entities;

public class MenuItemDietaryTag
{
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public int DietaryTagId { get; set; }
    public DietaryTag DietaryTag { get; set; } = null!;
}
