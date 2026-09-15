using Sofra.API.DTOs;
using Sofra.API.Entities;
using Sofra.API.Enums;

namespace Sofra.API.Services.Interfaces;

public interface IOrderStateMachine
{
    /// <summary>
    /// Provjerava da li je prelaz iz trenutnog statusa narudzbe u <paramref name="newStatus"/> dozvoljen
    /// za datog aktera (uloge + da li je vlasnik narudzbe), pa ga primjenjuje i upisuje audit polja.
    /// Baca BusinessException za nepostojeci prelaz u grafu, ForbiddenException ako akter nema ovlascenje.
    /// </summary>
    void Apply(Order order, OrderStatus newStatus, int actorUserId, IReadOnlyCollection<string> actorRoles, string? cancelReason = null);

    /// <summary>
    /// Statusi u koje akter upravo sada smije prevesti ovu narudzbu (uloga, vlasnistvo, placanje) -
    /// isti provjere kao Apply, samo bez izvrsavanja. Order.Payment mora biti ucitan.
    /// </summary>
    IReadOnlyList<AllowedTransition> GetAllowedTransitions(Order order, int actorUserId, IReadOnlyCollection<string> actorRoles);
}
