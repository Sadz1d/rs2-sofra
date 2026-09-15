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
}
