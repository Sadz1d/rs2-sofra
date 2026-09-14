namespace Sofra.API.Entities;

public class DietaryTag : INamedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
