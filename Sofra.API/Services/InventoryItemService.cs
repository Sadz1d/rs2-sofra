using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Inventory;
using Sofra.API.Entities;
using Sofra.API.Enums;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Inventory;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class InventoryItemService(AppDbContext dbContext) : IInventoryItemService
{
    private static readonly Expression<Func<InventoryItem, InventoryItemResponse>> ProjectToResponse = x => new InventoryItemResponse(
        x.Id, x.Name,
        x.InventoryCategoryId, x.InventoryCategory.Name,
        x.UnitOfMeasureId, x.UnitOfMeasure.Name, x.UnitOfMeasure.Abbreviation,
        x.Quantity, x.MinQuantity, x.UnitCost,
        x.Quantity <= x.MinQuantity, x.IsActive);

    public async Task<PagedResult<InventoryItemResponse>> GetListAsync(InventoryItemListRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.InventoryItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.Name.Contains(request.Search));
        }

        if (request.InventoryCategoryId.HasValue)
        {
            query = query.Where(x => x.InventoryCategoryId == request.InventoryCategoryId);
        }

        if (request.LowStock == true)
        {
            query = query.Where(x => x.Quantity <= x.MinQuantity);
        }

        query = request.SortDesc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name);

        return await query.Select(ProjectToResponse).ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<InventoryItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await dbContext.InventoryItems.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(ProjectToResponse)
            .FirstOrDefaultAsync(cancellationToken);

        return response ?? throw new NotFoundException($"Namirnica sa Id {id} ne postoji.");
    }

    public async Task<InventoryItemResponse> CreateAsync(InventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureReferencesExistAsync(request, cancellationToken);

        var entity = new InventoryItem
        {
            Name = request.Name,
            InventoryCategoryId = request.InventoryCategoryId,
            UnitOfMeasureId = request.UnitOfMeasureId,
            MinQuantity = request.MinQuantity,
            UnitCost = request.UnitCost,
            IsActive = request.IsActive,
            Quantity = 0,
        };

        dbContext.InventoryItems.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<InventoryItemResponse> UpdateAsync(int id, InventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.InventoryItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Namirnica sa Id {id} ne postoji.");

        await EnsureReferencesExistAsync(request, cancellationToken);

        entity.Name = request.Name;
        entity.InventoryCategoryId = request.InventoryCategoryId;
        entity.UnitOfMeasureId = request.UnitOfMeasureId;
        entity.MinQuantity = request.MinQuantity;
        entity.UnitCost = request.UnitCost;
        entity.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.InventoryItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Namirnica sa Id {id} ne postoji.");

        var usageCount = await dbContext.MenuItemIngredients.CountAsync(x => x.InventoryItemId == id, cancellationToken);
        if (usageCount > 0)
        {
            var noun = BosnianPluralizer.Pluralize(usageCount, "jelo", "jela", "jela");
            throw new BusinessException($"Namirnica '{entity.Name}' se ne može obrisati jer se koristi u normativu {usageCount} {noun}.");
        }

        // Soft delete (IsActive) - hard delete bi puko na Restrict FK-u iz InventoryTransaction historije.
        entity.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<InventoryItemResponse> AdjustAsync(int id, AdjustInventoryRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.InventoryItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Namirnica sa Id {id} ne postoji.");

        var newQuantity = request.Type switch
        {
            InventoryTransactionType.In => entity.Quantity + request.Quantity,
            InventoryTransactionType.Out or InventoryTransactionType.WriteOff => entity.Quantity - request.Quantity,
            _ => throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["Type"] = ["Tip promjene stanja nije validan."],
            }),
        };

        if (newQuantity < 0)
        {
            throw new BusinessException($"Stanje namirnice '{entity.Name}' ne može biti negativno (trenutno {entity.Quantity}, traženo umanjenje {request.Quantity}).");
        }

        entity.Quantity = newQuantity;

        dbContext.InventoryTransactions.Add(new InventoryTransaction
        {
            InventoryItemId = id,
            Type = request.Type,
            Quantity = request.Quantity,
            Note = request.Note,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task EnsureReferencesExistAsync(InventoryItemRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.InventoryCategories.AnyAsync(x => x.Id == request.InventoryCategoryId, cancellationToken))
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["InventoryCategoryId"] = [$"Kategorija namirnice sa Id {request.InventoryCategoryId} ne postoji."],
            });
        }

        if (!await dbContext.UnitsOfMeasure.AnyAsync(x => x.Id == request.UnitOfMeasureId, cancellationToken))
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["UnitOfMeasureId"] = [$"Jedinica mjere sa Id {request.UnitOfMeasureId} ne postoji."],
            });
        }
    }
}
