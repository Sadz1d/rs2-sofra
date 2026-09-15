using Sofra.API.DTOs;
using Sofra.API.DTOs.Shifts;
using Sofra.API.Requests.Shifts;

namespace Sofra.API.Services.Interfaces;

public interface IShiftService
{
    Task<PagedResult<ShiftResponse>> GetListAsync(ShiftListRequest request, CancellationToken cancellationToken = default);
    Task<ShiftResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ShiftResponse> CreateAsync(ShiftRequest request, CancellationToken cancellationToken = default);
    Task<ShiftResponse> UpdateAsync(int id, ShiftRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
