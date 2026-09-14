namespace Sofra.API.Services;

/// <summary>
/// Pravilan bosanski oblik imenice uz broj (1 jelo, 2-4 jela, 5+ jela, 11-14 uvijek "mnogo" oblik).
/// Zajednicki helper za poruke tipa "ne moze se obrisati jer je koristi N zapisa".
/// </summary>
public static class BosnianPluralizer
{
    public static string Pluralize(int count, string singular, string few, string many)
    {
        var mod100 = count % 100;
        var mod10 = count % 10;

        if (mod100 is >= 11 and <= 14)
        {
            return many;
        }

        return mod10 switch
        {
            1 => singular,
            >= 2 and <= 4 => few,
            _ => many,
        };
    }
}
