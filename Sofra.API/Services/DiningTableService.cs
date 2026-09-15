using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Tables;
using Sofra.API.Entities;
using Sofra.API.Enums;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Tables;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class DiningTableService(AppDbContext dbContext) : IDiningTableService
{
    private static readonly OrderStatus[] ActiveOrderStatuses =
    [
        OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.InPreparation, OrderStatus.Ready, OrderStatus.Delivered,
    ];

    private static readonly ReservationStatus[] ActiveReservationStatuses =
    [
        ReservationStatus.Pending, ReservationStatus.Confirmed,
    ];

    private static readonly Expression<Func<DiningTable, DiningTableResponse>> ProjectToResponse = x => new DiningTableResponse(
        x.Id, x.Number, x.Capacity,
        x.ZoneId, x.Zone.Name,
        x.TableTypeId, x.TableType.Name,
        x.Status,
        x.WaiterId, x.Waiter == null ? null : x.Waiter.FirstName + " " + x.Waiter.LastName,
        x.QrCode, x.IsActive);

    public async Task<PagedResult<DiningTableResponse>> GetListAsync(DiningTableListRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.DiningTables.AsNoTracking().AsQueryable();

        if (request.ZoneId.HasValue)
        {
            query = query.Where(x => x.ZoneId == request.ZoneId);
        }

        if (request.TableTypeId.HasValue)
        {
            query = query.Where(x => x.TableTypeId == request.TableTypeId);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status);
        }

        if (request.MinCapacity.HasValue)
        {
            query = query.Where(x => x.Capacity >= request.MinCapacity);
        }

        query = request.SortDesc ? query.OrderByDescending(x => x.Number) : query.OrderBy(x => x.Number);

        return await query.Select(ProjectToResponse).ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<DiningTableResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await dbContext.DiningTables.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(ProjectToResponse)
            .FirstOrDefaultAsync(cancellationToken);

        return response ?? throw new NotFoundException($"Sto sa Id {id} ne postoji.");
    }

    public async Task<DiningTableResponse> CreateAsync(DiningTableRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureReferencesExistAsync(request, cancellationToken);
        await EnsureNumberUniqueAsync(request.Number, excludeId: null, cancellationToken);

        var qrCode = string.IsNullOrWhiteSpace(request.QrCode) ? GenerateQrCode() : request.QrCode.Trim();
        if (!string.IsNullOrWhiteSpace(request.QrCode))
        {
            await EnsureQrCodeUniqueAsync(qrCode, excludeId: null, cancellationToken);
        }

        var entity = new DiningTable
        {
            Number = request.Number,
            Capacity = request.Capacity,
            ZoneId = request.ZoneId,
            TableTypeId = request.TableTypeId,
            WaiterId = request.WaiterId,
            QrCode = qrCode,
            Status = request.Status,
            IsActive = true,
        };

        dbContext.DiningTables.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<DiningTableResponse> UpdateAsync(int id, DiningTableRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.DiningTables.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Sto sa Id {id} ne postoji.");

        await EnsureReferencesExistAsync(request, cancellationToken);
        await EnsureNumberUniqueAsync(request.Number, excludeId: id, cancellationToken);

        var qrCode = string.IsNullOrWhiteSpace(request.QrCode) ? entity.QrCode : request.QrCode.Trim();
        if (!string.Equals(qrCode, entity.QrCode, StringComparison.Ordinal))
        {
            await EnsureQrCodeUniqueAsync(qrCode, excludeId: id, cancellationToken);
        }

        entity.Number = request.Number;
        entity.Capacity = request.Capacity;
        entity.ZoneId = request.ZoneId;
        entity.TableTypeId = request.TableTypeId;
        entity.WaiterId = request.WaiterId;
        entity.QrCode = qrCode;
        entity.Status = request.Status;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.DiningTables.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Sto sa Id {id} ne postoji.");

        var activeOrders = await dbContext.Orders.CountAsync(x => x.DiningTableId == id && ActiveOrderStatuses.Contains(x.Status), cancellationToken);
        if (activeOrders > 0)
        {
            var noun = BosnianPluralizer.Pluralize(activeOrders, "aktivnu narudžbu", "aktivne narudžbe", "aktivnih narudžbi");
            throw new BusinessException($"Sto broj {entity.Number} se ne može obrisati jer ima {activeOrders} {noun}.");
        }

        var activeReservations = await dbContext.Reservations.CountAsync(x => x.DiningTableId == id && ActiveReservationStatuses.Contains(x.Status), cancellationToken);
        if (activeReservations > 0)
        {
            var noun = BosnianPluralizer.Pluralize(activeReservations, "aktivnu rezervaciju", "aktivne rezervacije", "aktivnih rezervacija");
            throw new BusinessException($"Sto broj {entity.Number} se ne može obrisati jer ima {activeReservations} {noun}.");
        }

        // Soft delete (IsActive) - hard delete bi puko na Restrict FK-u iz historijskih narudzbi/rezervacija.
        entity.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateQrCode() => $"TBL-{Guid.NewGuid():N}"[..16].ToUpperInvariant();

    private async Task EnsureReferencesExistAsync(DiningTableRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Zones.AnyAsync(x => x.Id == request.ZoneId, cancellationToken))
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["ZoneId"] = [$"Zona sa Id {request.ZoneId} ne postoji."],
            });
        }

        if (!await dbContext.TableTypes.AnyAsync(x => x.Id == request.TableTypeId, cancellationToken))
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["TableTypeId"] = [$"Tip stola sa Id {request.TableTypeId} ne postoji."],
            });
        }

        if (request.WaiterId.HasValue && !await dbContext.Users.AnyAsync(x => x.Id == request.WaiterId, cancellationToken))
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["WaiterId"] = [$"Korisnik sa Id {request.WaiterId} ne postoji."],
            });
        }
    }

    private async Task EnsureNumberUniqueAsync(int number, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.DiningTables.AnyAsync(x => x.Number == number && (excludeId == null || x.Id != excludeId), cancellationToken);
        if (exists)
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["Number"] = [$"Sto sa brojem {number} već postoji."],
            });
        }
    }

    private async Task EnsureQrCodeUniqueAsync(string qrCode, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.DiningTables.AnyAsync(x => x.QrCode == qrCode && (excludeId == null || x.Id != excludeId), cancellationToken);
        if (exists)
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["QrCode"] = [$"QR kod '{qrCode}' je već u upotrebi."],
            });
        }
    }
}
