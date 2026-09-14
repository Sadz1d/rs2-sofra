using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Catalog;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class TableTypeService(AppDbContext dbContext)
    : LookupService<TableType, TableTypeResponse, TableTypeRequest>(dbContext), ITableTypeService
{
    protected override DbSet<TableType> Set => DbContext.TableTypes;

    protected override string EntityLabel => "Tip stola";

    protected override Expression<Func<TableType, TableTypeResponse>> ProjectToResponse =>
        x => new TableTypeResponse(x.Id, x.Name);

    protected override TableType CreateEntity(TableTypeRequest request) => new() { Name = request.Name };

    protected override void UpdateEntity(TableType entity, TableTypeRequest request) => entity.Name = request.Name;

    protected override async Task EnsureUniqueAsync(TableTypeRequest request, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await DbContext.TableTypes
            .AnyAsync(x => x.Name == request.Name && (excludeId == null || x.Id != excludeId), cancellationToken);

        if (exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Name"] = [$"Tip stola sa nazivom '{request.Name}' već postoji."],
            });
        }
    }

    protected override async Task<LookupUsage> GetUsageAsync(int id, CancellationToken cancellationToken)
    {
        var count = await DbContext.DiningTables.CountAsync(x => x.TableTypeId == id, cancellationToken);
        return new LookupUsage(count, "sto", "stola", "stolova");
    }
}
