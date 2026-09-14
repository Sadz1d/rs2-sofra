namespace Sofra.API.Entities;

public class InventoryCategory : INamedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
