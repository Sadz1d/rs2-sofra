namespace Sofra.Worker.Entities;

/// <summary>Mapira istu tabelu koju kreira API-jeva migracija - koristi se za idempotentnu obradu poruka.</summary>
public class ProcessedEvent
{
    public Guid EventId { get; set; }
    public DateTime ProcessedAt { get; set; }
}
