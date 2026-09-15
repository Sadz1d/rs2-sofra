namespace Sofra.API.Services;

/// <summary>
/// Uskladjuje trackovanu M:N kolekciju (npr. MenuItem.MenuItemAllergens) sa zeljenim skupom kljuceva
/// dodavajuci samo nove i uklanjajuci samo izostavljene - nikad ne brise pa ponovo ubacuje sve,
/// da se ne generisu nepotrebni DELETE+INSERT za redove koji ostaju nepromijenjeni.
/// </summary>
public static class CollectionSyncHelper
{
    public static void Sync<TJoin, TKey>(
        ICollection<TJoin> current,
        IEnumerable<TKey> desiredKeys,
        Func<TJoin, TKey> keySelector,
        Func<TKey, TJoin> factory)
    {
        var desired = desiredKeys.ToHashSet();

        foreach (var item in current.Where(x => !desired.Contains(keySelector(x))).ToList())
        {
            current.Remove(item);
        }

        var existing = current.Select(keySelector).ToHashSet();
        foreach (var key in desired.Where(k => !existing.Contains(k)))
        {
            current.Add(factory(key));
        }
    }
}
