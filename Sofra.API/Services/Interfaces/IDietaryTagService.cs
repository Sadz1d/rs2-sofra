using Sofra.API.DTOs;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Requests;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Services.Interfaces;

public interface IDietaryTagService
{
    Task<PagedResult<DietaryTagResponse>> GetListAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<DietaryTagResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DietaryTagResponse> CreateAsync(DietaryTagRequest request, CancellationToken cancellationToken = default);
    Task<DietaryTagResponse> UpdateAsync(int id, DietaryTagRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
