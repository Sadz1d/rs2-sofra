using Sofra.API.DTOs;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Services.Interfaces;

public interface IMenuCategoryService
{
    Task<PagedResult<MenuCategoryResponse>> GetListAsync(MenuCategoryListRequest request, CancellationToken cancellationToken = default);
    Task<MenuCategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<MenuCategoryResponse> CreateAsync(MenuCategoryRequest request, CancellationToken cancellationToken = default);
    Task<MenuCategoryResponse> UpdateAsync(int id, MenuCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
