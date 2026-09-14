namespace Sofra.API.Entities;

public interface IEntity
{
    int Id { get; }
}

/// <summary>
/// Implementiraju je svi sifarnici - omogucava generickoj LookupService bazi da pretrazuje/sortira
/// po Name bez poznavanja konkretnog tipa.
/// </summary>
public interface INamedEntity : IEntity
{
    string Name { get; }
}
