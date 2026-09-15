using Sofra.API.Entities;
using Sofra.API.Enums;

namespace Sofra.API.Services.Interfaces;

public interface IReservationStateMachine
{
    /// <summary>
    /// Provjerava da li je prelaz iz trenutnog statusa rezervacije u <paramref name="newStatus"/> dozvoljen
    /// za datog aktera (uloge + vlasnistvo), pa ga primjenjuje i upisuje audit polja. Baca BusinessException
    /// za nepostojeci prelaz u grafu (ili gost koji pokusava otkazati nakon pocetka termina), ForbiddenException
    /// ako akter nema ovlascenje, ValidationException ako nedostaje obavezan RejectReason.
    /// </summary>
    void Apply(
        Reservation reservation, ReservationStatus newStatus, int actorUserId, IReadOnlyCollection<string> actorRoles,
        string? rejectReason = null, DateTime? alternativeAt = null);
}
