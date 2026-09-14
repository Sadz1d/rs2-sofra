namespace Sofra.API.Entities;

public class Country : INamedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public ICollection<City> Cities { get; set; } = new List<City>();
}
