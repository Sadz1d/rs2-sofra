namespace Sofra.Shared.Events;

/// <summary>Zajednicki ugovor za sve dogadjaje objavljene na sofra.events - EventId omogucava idempotentnu obradu na strani Workera.</summary>
public interface IEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
