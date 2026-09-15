namespace Sofra.API.Entities;

/// <summary>
/// Zapisuje EventId vec obradjenih dogadjaja - koristi ga iskljucivo Worker za idempotentnu obradu
/// (ista poruka isporucena dva puta ne smije napraviti dva zapisa). Tabelu kreira API-jeva migracija,
/// Worker ima svoj minimalni DbContext koji mapira istu tabelu.
/// </summary>
public class ProcessedEvent
{
    public Guid EventId { get; set; }
    public DateTime ProcessedAt { get; set; }
}
