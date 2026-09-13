namespace Sofra.API.Entities;

public class MenuItemPair
{
    public int Id { get; set; }
    public int MenuItemAId { get; set; }
    public MenuItem MenuItemA { get; set; } = null!;
    public int MenuItemBId { get; set; }
    public MenuItem MenuItemB { get; set; } = null!;
    public int PairCount { get; set; }
    public DateTime ComputedAt { get; set; }
}
