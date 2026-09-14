using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Catalog;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class AllergenService(AppDbContext dbContext)
    : LookupService<Allergen, AllergenResponse, AllergenRequest>(dbContext), IAllergenService
{
    protected override DbSet<Allergen> Set => DbContext.Allergens;

    protected override string EntityLabel => "Alergen";

    protected override Expression<Func<Allergen, AllergenResponse>> ProjectToResponse =>
        x => new AllergenResponse(x.Id, x.Name);

    protected override Allergen CreateEntity(AllergenRequest request) => new() { Name = request.Name };

    protected override void UpdateEntity(Allergen entity, AllergenRequest request) => entity.Name = request.Name;

    protected override async Task EnsureUniqueAsync(AllergenRequest request, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await DbContext.Allergens
            .AnyAsync(x => x.Name == request.Name && (excludeId == null || x.Id != excludeId), cancellationToken);

        if (exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Name"] = [$"Alergen sa nazivom '{request.Name}' već postoji."],
            });
        }
    }

    protected override async Task<LookupUsage> GetUsageAsync(int id, CancellationToken cancellationToken)
    {
        var count = await DbContext.MenuItemAllergens.CountAsync(x => x.AllergenId == id, cancellationToken);
        return new LookupUsage(count, "jelo", "jela", "jela");
    }
}
