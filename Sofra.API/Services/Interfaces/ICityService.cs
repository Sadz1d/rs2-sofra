using Sofra.API.DTOs;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Requests;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Services.Interfaces;

public interface ICityService
{
    Task<PagedResult<CityResponse>> GetListAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<CityResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CityResponse> CreateAsync(CityRequest request, CancellationToken cancellationToken = default);
    Task<CityResponse> UpdateAsync(int id, CityRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
