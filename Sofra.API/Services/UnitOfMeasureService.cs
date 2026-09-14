using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Catalog;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class UnitOfMeasureService(AppDbContext dbContext)
    : LookupService<UnitOfMeasure, UnitOfMeasureResponse, UnitOfMeasureRequest>(dbContext), IUnitOfMeasureService
{
    protected override DbSet<UnitOfMeasure> Set => DbContext.UnitsOfMeasure;

    protected override string EntityLabel => "Jedinica mjere";

    protected override Expression<Func<UnitOfMeasure, UnitOfMeasureResponse>> ProjectToResponse =>
        x => new UnitOfMeasureResponse(x.Id, x.Name, x.Abbreviation);

    protected override UnitOfMeasure CreateEntity(UnitOfMeasureRequest request) => new()
    {
        Name = request.Name,
        Abbreviation = request.Abbreviation,
    };

    protected override void UpdateEntity(UnitOfMeasure entity, UnitOfMeasureRequest request)
    {
        entity.Name = request.Name;
        entity.Abbreviation = request.Abbreviation;
    }

    // Unique ogranicenje u bazi je na Abbreviation (ne Name) - vidi UnitOfMeasureConfiguration.
    protected override async Task EnsureUniqueAsync(UnitOfMeasureRequest request, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await DbContext.UnitsOfMeasure
            .AnyAsync(x => x.Abbreviation == request.Abbreviation && (excludeId == null || x.Id != excludeId), cancellationToken);

        if (exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Abbreviation"] = [$"Jedinica mjere sa skraćenicom '{request.Abbreviation}' već postoji."],
            });
        }
    }

    protected override async Task<LookupUsage> GetUsageAsync(int id, CancellationToken cancellationToken)
    {
        var count = await DbContext.InventoryItems.CountAsync(x => x.UnitOfMeasureId == id, cancellationToken);
        return new LookupUsage(count, "namirnica", "namirnice", "namirnica");
    }
}
