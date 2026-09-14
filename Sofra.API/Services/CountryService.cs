using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Catalog;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class CountryService(AppDbContext dbContext)
    : LookupService<Country, CountryResponse, CountryRequest>(dbContext), ICountryService
{
    protected override DbSet<Country> Set => DbContext.Countries;

    protected override string EntityLabel => "Država";

    protected override Expression<Func<Country, CountryResponse>> ProjectToResponse =>
        x => new CountryResponse(x.Id, x.Name, x.Code);

    protected override Country CreateEntity(CountryRequest request) => new() { Name = request.Name, Code = request.Code };

    protected override void UpdateEntity(Country entity, CountryRequest request)
    {
        entity.Name = request.Name;
        entity.Code = request.Code;
    }

    protected override async Task EnsureUniqueAsync(CountryRequest request, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await DbContext.Countries
            .AnyAsync(x => x.Name == request.Name && (excludeId == null || x.Id != excludeId), cancellationToken);

        if (exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Name"] = [$"Država sa nazivom '{request.Name}' već postoji."],
            });
        }
    }

    protected override async Task<LookupUsage> GetUsageAsync(int id, CancellationToken cancellationToken)
    {
        var count = await DbContext.Cities.CountAsync(x => x.CountryId == id, cancellationToken);
        return new LookupUsage(count, "grad", "grada", "gradova");
    }
}
