using Sofra.API.DTOs;
using Sofra.API.DTOs.Inventory;
using Sofra.API.Requests.Inventory;

namespace Sofra.API.Services.Interfaces;

public interface IInventoryItemService
{
    Task<PagedResult<InventoryItemResponse>> GetListAsync(InventoryItemListRequest request, CancellationToken cancellationToken = default);
    Task<InventoryItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InventoryItemResponse> CreateAsync(InventoryItemRequest request, CancellationToken cancellationToken = default);
    Task<InventoryItemResponse> UpdateAsync(int id, InventoryItemRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<InventoryItemResponse> AdjustAsync(int id, AdjustInventoryRequest request, int userId, CancellationToken cancellationToken = default);
}
