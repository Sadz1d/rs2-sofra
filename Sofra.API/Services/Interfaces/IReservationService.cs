using Sofra.API.DTOs;
using Sofra.API.DTOs.Reservations;
using Sofra.API.Requests.Reservations;

namespace Sofra.API.Services.Interfaces;

public interface IReservationService
{
    Task<PagedResult<ReservationListItemResponse>> GetListAsync(ReservationListRequest request, int actorUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task<ReservationResponse> GetByIdAsync(int id, int actorUserId, IReadOnlyCollection<string> actorRoles, bool isStaff, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReservationSlotResponse>> GetAvailabilityAsync(ReservationAvailabilityRequest request, CancellationToken cancellationToken = default);
    Task<ReservationResponse> CreateAsync(ReservationRequest request, int actorUserId, IReadOnlyCollection<string> actorRoles, CancellationToken cancellationToken = default);
    Task<ReservationResponse> TransitionAsync(int id, ReservationTransitionRequest request, int actorUserId, IReadOnlyCollection<string> actorRoles, CancellationToken cancellationToken = default);
    Task<ReservationResponse> AssignTableAsync(int id, AssignReservationTableRequest request, int actorUserId, IReadOnlyCollection<string> actorRoles, CancellationToken cancellationToken = default);
}
