using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Catalog;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class PaymentMethodService(AppDbContext dbContext)
    : LookupService<PaymentMethod, PaymentMethodResponse, PaymentMethodRequest>(dbContext), IPaymentMethodService
{
    protected override DbSet<PaymentMethod> Set => DbContext.PaymentMethods;

    protected override string EntityLabel => "Način plaćanja";

    protected override Expression<Func<PaymentMethod, PaymentMethodResponse>> ProjectToResponse =>
        x => new PaymentMethodResponse(x.Id, x.Name, x.Code);

    protected override PaymentMethod CreateEntity(PaymentMethodRequest request) => new() { Name = request.Name, Code = request.Code };

    protected override void UpdateEntity(PaymentMethod entity, PaymentMethodRequest request)
    {
        entity.Name = request.Name;
        entity.Code = request.Code;
    }

    protected override async Task EnsureUniqueAsync(PaymentMethodRequest request, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await DbContext.PaymentMethods
            .AnyAsync(x => x.Name == request.Name && (excludeId == null || x.Id != excludeId), cancellationToken);

        if (exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Name"] = [$"Način plaćanja sa nazivom '{request.Name}' već postoji."],
            });
        }
    }

    protected override async Task<LookupUsage> GetUsageAsync(int id, CancellationToken cancellationToken)
    {
        var count = await DbContext.Payments.CountAsync(x => x.PaymentMethodId == id, cancellationToken);
        return new LookupUsage(count, "plaćanje", "plaćanja", "plaćanja");
    }
}
