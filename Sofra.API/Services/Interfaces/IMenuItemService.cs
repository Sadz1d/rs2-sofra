using Sofra.API.DTOs;
using Sofra.API.DTOs.Menu;
using Sofra.API.Requests.Menu;

namespace Sofra.API.Services.Interfaces;

public interface IMenuItemService
{
    Task<PagedResult<MenuItemResponse>> GetListAsync(MenuItemListRequest request, CancellationToken cancellationToken = default);
    Task<MenuItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<MenuItemResponse> CreateAsync(MenuItemRequest request, CancellationToken cancellationToken = default);
    Task<MenuItemResponse> UpdateAsync(int id, MenuItemRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MenuItemIngredientResponse>> GetIngredientsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuItemIngredientResponse>> UpdateIngredientsAsync(int id, UpdateMenuItemIngredientsRequest request, CancellationToken cancellationToken = default);
}
