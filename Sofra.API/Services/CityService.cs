using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests;
using Sofra.API.Requests.Catalog;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class CityService(AppDbContext dbContext)
    : LookupService<City, CityResponse, CityRequest>(dbContext), ICityService
{
    protected override DbSet<City> Set => DbContext.Cities;

    protected override string EntityLabel => "Grad";

    // Ukljucuje naziv drzave preko JOIN-a (Select translira u SQL, nema N+1, bez Include-a).
    protected override Expression<Func<City, CityResponse>> ProjectToResponse =>
        x => new CityResponse(x.Id, x.Name, x.CountryId, x.Country.Name);

    protected override IQueryable<City> ApplyExtraFilters(IQueryable<City> query, PagedRequest request) =>
        request is CityListRequest { CountryId: not null } r ? query.Where(x => x.CountryId == r.CountryId) : query;

    protected override City CreateEntity(CityRequest request) => new() { Name = request.Name, CountryId = request.CountryId };

    protected override void UpdateEntity(City entity, CityRequest request)
    {
        entity.Name = request.Name;
        entity.CountryId = request.CountryId;
    }

    // Unique je kompozitno (Name, CountryId) - vidi CityConfiguration. Provjerava se i da drzava postoji,
    // da FK ne baci gresku baze nego jasnu poruku.
    protected override async Task EnsureUniqueAsync(CityRequest request, int? excludeId, CancellationToken cancellationToken)
    {
        var countryExists = await DbContext.Countries.AnyAsync(x => x.Id == request.CountryId, cancellationToken);
        if (!countryExists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["CountryId"] = [$"Država sa Id {request.CountryId} ne postoji."],
            });
        }

        var exists = await DbContext.Cities.AnyAsync(
            x => x.Name == request.Name && x.CountryId == request.CountryId && (excludeId == null || x.Id != excludeId),
            cancellationToken);

        if (exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Name"] = [$"Grad sa nazivom '{request.Name}' već postoji u odabranoj državi."],
            });
        }
    }

    protected override async Task<LookupUsage> GetUsageAsync(int id, CancellationToken cancellationToken)
    {
        var count = await DbContext.Users.CountAsync(x => x.CityId == id, cancellationToken);
        return new LookupUsage(count, "korisnik", "korisnika", "korisnika");
    }
}
