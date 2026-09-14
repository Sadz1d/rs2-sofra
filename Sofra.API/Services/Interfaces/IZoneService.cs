using Sofra.API.DTOs;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Requests;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Services.Interfaces;

public interface IZoneService
{
    Task<PagedResult<ZoneResponse>> GetListAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<ZoneResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ZoneResponse> CreateAsync(ZoneRequest request, CancellationToken cancellationToken = default);
    Task<ZoneResponse> UpdateAsync(int id, ZoneRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
