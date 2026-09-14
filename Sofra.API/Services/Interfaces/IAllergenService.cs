using Sofra.API.DTOs;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Requests;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Services.Interfaces;

public interface IAllergenService
{
    Task<PagedResult<AllergenResponse>> GetListAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<AllergenResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AllergenResponse> CreateAsync(AllergenRequest request, CancellationToken cancellationToken = default);
    Task<AllergenResponse> UpdateAsync(int id, AllergenRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
