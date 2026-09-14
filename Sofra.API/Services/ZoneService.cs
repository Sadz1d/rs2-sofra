using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Catalog;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class ZoneService(AppDbContext dbContext)
    : LookupService<Zone, ZoneResponse, ZoneRequest>(dbContext), IZoneService
{
    protected override DbSet<Zone> Set => DbContext.Zones;

    protected override string EntityLabel => "Zona";

    protected override Expression<Func<Zone, ZoneResponse>> ProjectToResponse =>
        x => new ZoneResponse(x.Id, x.Name, x.Capacity);

    protected override Zone CreateEntity(ZoneRequest request) => new() { Name = request.Name, Capacity = request.Capacity };

    protected override void UpdateEntity(Zone entity, ZoneRequest request)
    {
        entity.Name = request.Name;
        entity.Capacity = request.Capacity;
    }

    protected override async Task EnsureUniqueAsync(ZoneRequest request, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await DbContext.Zones
            .AnyAsync(x => x.Name == request.Name && (excludeId == null || x.Id != excludeId), cancellationToken);

        if (exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Name"] = [$"Zona sa nazivom '{request.Name}' već postoji."],
            });
        }
    }

    protected override async Task<LookupUsage> GetUsageAsync(int id, CancellationToken cancellationToken)
    {
        var tableCount = await DbContext.DiningTables.CountAsync(x => x.ZoneId == id, cancellationToken);
        if (tableCount > 0)
        {
            return new LookupUsage(tableCount, "sto", "stola", "stolova");
        }

        var reservationCount = await DbContext.Reservations.CountAsync(x => x.ZoneId == id, cancellationToken);
        return new LookupUsage(reservationCount, "rezervacija", "rezervacije", "rezervacija");
    }
}
