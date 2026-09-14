using Sofra.API.DTOs;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Requests;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Services.Interfaces;

public interface ITableTypeService
{
    Task<PagedResult<TableTypeResponse>> GetListAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<TableTypeResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TableTypeResponse> CreateAsync(TableTypeRequest request, CancellationToken cancellationToken = default);
    Task<TableTypeResponse> UpdateAsync(int id, TableTypeRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
