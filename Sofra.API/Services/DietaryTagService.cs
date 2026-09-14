using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Catalog;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class DietaryTagService(AppDbContext dbContext)
    : LookupService<DietaryTag, DietaryTagResponse, DietaryTagRequest>(dbContext), IDietaryTagService
{
    protected override DbSet<DietaryTag> Set => DbContext.DietaryTags;

    protected override string EntityLabel => "Prehrambena oznaka";

    protected override Expression<Func<DietaryTag, DietaryTagResponse>> ProjectToResponse =>
        x => new DietaryTagResponse(x.Id, x.Name);

    protected override DietaryTag CreateEntity(DietaryTagRequest request) => new() { Name = request.Name };

    protected override void UpdateEntity(DietaryTag entity, DietaryTagRequest request) => entity.Name = request.Name;

    protected override async Task EnsureUniqueAsync(DietaryTagRequest request, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await DbContext.DietaryTags
            .AnyAsync(x => x.Name == request.Name && (excludeId == null || x.Id != excludeId), cancellationToken);

        if (exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Name"] = [$"Oznaka sa nazivom '{request.Name}' već postoji."],
            });
        }
    }

    protected override async Task<LookupUsage> GetUsageAsync(int id, CancellationToken cancellationToken)
    {
        var count = await DbContext.MenuItemDietaryTags.CountAsync(x => x.DietaryTagId == id, cancellationToken);
        return new LookupUsage(count, "jelo", "jela", "jela");
    }
}
