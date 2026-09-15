using Sofra.API.DTOs;
using Sofra.API.DTOs.Orders;
using Sofra.API.Requests.Orders;

namespace Sofra.API.Services.Interfaces;

public interface IOrderService
{
    Task<PagedResult<OrderListItemResponse>> GetListAsync(OrderListRequest request, int actorUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task<OrderResponse> GetByIdAsync(int id, int actorUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task<OrderQuoteResponse> QuoteAsync(PlaceOrderRequest request, int actorUserId, CancellationToken cancellationToken = default);
    Task<OrderResponse> CreateAsync(PlaceOrderRequest request, int actorUserId, CancellationToken cancellationToken = default);
    Task<OrderResponse> TransitionAsync(int id, OrderTransitionRequest request, int actorUserId, IReadOnlyCollection<string> actorRoles, CancellationToken cancellationToken = default);
}
