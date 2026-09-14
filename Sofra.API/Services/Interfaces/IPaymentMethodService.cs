using Sofra.API.DTOs;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Requests;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Services.Interfaces;

public interface IPaymentMethodService
{
    Task<PagedResult<PaymentMethodResponse>> GetListAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<PaymentMethodResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PaymentMethodResponse> CreateAsync(PaymentMethodRequest request, CancellationToken cancellationToken = default);
    Task<PaymentMethodResponse> UpdateAsync(int id, PaymentMethodRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
