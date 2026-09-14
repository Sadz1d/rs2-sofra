using Sofra.API.DTOs;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Requests;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Services.Interfaces;

public interface IInventoryCategoryService
{
    Task<PagedResult<InventoryCategoryResponse>> GetListAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<InventoryCategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InventoryCategoryResponse> CreateAsync(InventoryCategoryRequest request, CancellationToken cancellationToken = default);
    Task<InventoryCategoryResponse> UpdateAsync(int id, InventoryCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
