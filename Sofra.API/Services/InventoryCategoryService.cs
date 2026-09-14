using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Catalog;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class InventoryCategoryService(AppDbContext dbContext)
    : LookupService<InventoryCategory, InventoryCategoryResponse, InventoryCategoryRequest>(dbContext), IInventoryCategoryService
{
    protected override DbSet<InventoryCategory> Set => DbContext.InventoryCategories;

    protected override string EntityLabel => "Kategorija namirnica";

    protected override Expression<Func<InventoryCategory, InventoryCategoryResponse>> ProjectToResponse =>
        x => new InventoryCategoryResponse(x.Id, x.Name);

    protected override InventoryCategory CreateEntity(InventoryCategoryRequest request) => new() { Name = request.Name };

    protected override void UpdateEntity(InventoryCategory entity, InventoryCategoryRequest request) => entity.Name = request.Name;

    protected override async Task EnsureUniqueAsync(InventoryCategoryRequest request, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await DbContext.InventoryCategories
            .AnyAsync(x => x.Name == request.Name && (excludeId == null || x.Id != excludeId), cancellationToken);

        if (exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Name"] = [$"Kategorija namirnica sa nazivom '{request.Name}' već postoji."],
            });
        }
    }

    protected override async Task<LookupUsage> GetUsageAsync(int id, CancellationToken cancellationToken)
    {
        var count = await DbContext.InventoryItems.CountAsync(x => x.InventoryCategoryId == id, cancellationToken);
        return new LookupUsage(count, "namirnica", "namirnice", "namirnica");
    }
}
