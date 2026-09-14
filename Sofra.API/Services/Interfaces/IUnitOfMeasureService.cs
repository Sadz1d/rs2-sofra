using Sofra.API.DTOs;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Requests;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Services.Interfaces;

public interface IUnitOfMeasureService
{
    Task<PagedResult<UnitOfMeasureResponse>> GetListAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<UnitOfMeasureResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UnitOfMeasureResponse> CreateAsync(UnitOfMeasureRequest request, CancellationToken cancellationToken = default);
    Task<UnitOfMeasureResponse> UpdateAsync(int id, UnitOfMeasureRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
