using Sofra.API.DTOs;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Requests;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Services.Interfaces;

public interface ICountryService
{
    Task<PagedResult<CountryResponse>> GetListAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<CountryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CountryResponse> CreateAsync(CountryRequest request, CancellationToken cancellationToken = default);
    Task<CountryResponse> UpdateAsync(int id, CountryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
