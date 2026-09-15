using Sofra.API.DTOs;
using Sofra.API.DTOs.Promotions;
using Sofra.API.Requests.Promotions;

namespace Sofra.API.Services.Interfaces;

public interface IPromotionService
{
    Task<PagedResult<PromotionResponse>> GetListAsync(PromotionListRequest request, CancellationToken cancellationToken = default);
    Task<PromotionResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PromotionResponse> CreateAsync(PromotionRequest request, CancellationToken cancellationToken = default);
    Task<PromotionResponse> UpdateAsync(int id, PromotionRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
