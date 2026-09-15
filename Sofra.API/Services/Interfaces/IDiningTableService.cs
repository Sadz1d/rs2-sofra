using Sofra.API.DTOs;
using Sofra.API.DTOs.Tables;
using Sofra.API.Requests.Tables;

namespace Sofra.API.Services.Interfaces;

public interface IDiningTableService
{
    Task<PagedResult<DiningTableResponse>> GetListAsync(DiningTableListRequest request, CancellationToken cancellationToken = default);
    Task<DiningTableResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DiningTableResponse> CreateAsync(DiningTableRequest request, CancellationToken cancellationToken = default);
    Task<DiningTableResponse> UpdateAsync(int id, DiningTableRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
