namespace Sofra.Shared.Events;

/// <summary>Zahtjev za reset lozinke - ResetCode je plain-text (u bazi se cuva samo hash), Worker ga samo prosljedjuje u e-mail.</summary>
public record PasswordResetRequestedEvent(
    Guid EventId,
    DateTime OccurredAt,
    int UserId,
    string UserEmail,
    string UserName,
    string ResetCode,
    DateTime ExpiresAt) : IEvent;
