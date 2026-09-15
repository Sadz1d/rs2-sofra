using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Catalog;
using Sofra.API.DTOs.Menu;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Menu;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class MenuItemService(AppDbContext dbContext) : IMenuItemService
{
    private static readonly Expression<Func<MenuItem, MenuItemResponse>> ProjectToResponse = x => new MenuItemResponse(
        x.Id,
        x.Name,
        x.Description,
        x.Price,
        x.ImageUrl,
        x.MenuCategoryId,
        x.MenuCategory.Name,
        x.ServingPeriods,
        x.IsAvailable,
        x.AvgRating,
        x.ReviewCount,
        x.MenuItemAllergens.Select(a => new AllergenResponse(a.Allergen.Id, a.Allergen.Name)).ToList(),
        x.MenuItemDietaryTags.Select(t => new DietaryTagResponse(t.DietaryTag.Id, t.DietaryTag.Name)).ToList());

    public async Task<PagedResult<MenuItemResponse>> GetListAsync(MenuItemListRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.MenuItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.Name.Contains(request.Search) || (x.Description != null && x.Description.Contains(request.Search)));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(x => x.MenuCategoryId == request.CategoryId);
        }

        if (request.IsAvailable.HasValue)
        {
            query = query.Where(x => x.IsAvailable == request.IsAvailable);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(x => x.Price >= request.MinPrice);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(x => x.Price <= request.MaxPrice);
        }

        if (request.AllergenId.HasValue)
        {
            query = query.Where(x => !x.MenuItemAllergens.Any(a => a.AllergenId == request.AllergenId));
        }

        if (request.DietaryTagId.HasValue)
        {
            query = query.Where(x => x.MenuItemDietaryTags.Any(t => t.DietaryTagId == request.DietaryTagId));
        }

        if (request.ServingPeriod.HasValue)
        {
            query = query.Where(x => (x.ServingPeriods & request.ServingPeriod.Value) == request.ServingPeriod.Value);
        }

        query = ApplySort(query, request.SortBy, request.SortDesc);

        return await query.Select(ProjectToResponse).ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<MenuItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await dbContext.MenuItems.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(ProjectToResponse)
            .FirstOrDefaultAsync(cancellationToken);

        return response ?? throw new NotFoundException($"Jelo sa Id {id} ne postoji.");
    }

    public async Task<MenuItemResponse> CreateAsync(MenuItemRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCategoryExistsAsync(request.MenuCategoryId, cancellationToken);
        await EnsureReferencesExistAsync(request.AllergenIds, request.DietaryTagIds, cancellationToken);

        var entity = new MenuItem
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            MenuCategoryId = request.MenuCategoryId,
            ServingPeriods = request.ServingPeriods,
            IsAvailable = request.IsAvailable,
        };

        SyncAllergens(entity, request.AllergenIds);
        SyncDietaryTags(entity, request.DietaryTagIds);

        dbContext.MenuItems.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<MenuItemResponse> UpdateAsync(int id, MenuItemRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.MenuItems
            .Include(x => x.MenuItemAllergens)
            .Include(x => x.MenuItemDietaryTags)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Jelo sa Id {id} ne postoji.");

        await EnsureCategoryExistsAsync(request.MenuCategoryId, cancellationToken);
        await EnsureReferencesExistAsync(request.AllergenIds, request.DietaryTagIds, cancellationToken);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Price = request.Price;
        entity.MenuCategoryId = request.MenuCategoryId;
        entity.ServingPeriods = request.ServingPeriods;
        entity.IsAvailable = request.IsAvailable;

        SyncAllergens(entity, request.AllergenIds);
        SyncDietaryTags(entity, request.DietaryTagIds);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.MenuItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Jelo sa Id {id} ne postoji.");

        // Soft delete - jelo ostaje vezano za stare narudzbe/recenzije; global query filter (IsDeleted)
        // ga automatski izostavlja iz svih buducih upita.
        entity.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuItemIngredientResponse>> GetIngredientsAsync(int id, CancellationToken cancellationToken = default)
    {
        await EnsureMenuItemExistsAsync(id, cancellationToken);

        return await dbContext.MenuItemIngredients.AsNoTracking()
            .Where(x => x.MenuItemId == id)
            .Select(x => new MenuItemIngredientResponse(x.InventoryItemId, x.InventoryItem.Name, x.Quantity, x.InventoryItem.UnitOfMeasure.Abbreviation))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuItemIngredientResponse>> UpdateIngredientsAsync(int id, UpdateMenuItemIngredientsRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureMenuItemExistsAsync(id, cancellationToken);

        var inventoryItemIds = request.Ingredients.Select(x => x.InventoryItemId).ToList();
        var existingCount = await dbContext.InventoryItems.CountAsync(x => inventoryItemIds.Contains(x.Id), cancellationToken);
        if (existingCount != inventoryItemIds.Distinct().Count())
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["Ingredients"] = ["Jedna ili više navedenih namirnica ne postoji."],
            });
        }

        var existing = dbContext.MenuItemIngredients.Where(x => x.MenuItemId == id);
        dbContext.MenuItemIngredients.RemoveRange(existing);

        dbContext.MenuItemIngredients.AddRange(request.Ingredients.Select(line => new MenuItemIngredient
        {
            MenuItemId = id,
            InventoryItemId = line.InventoryItemId,
            Quantity = line.Quantity,
        }));

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetIngredientsAsync(id, cancellationToken);
    }

    private static IQueryable<MenuItem> ApplySort(IQueryable<MenuItem> query, string? sortBy, bool descending)
    {
        Expression<Func<MenuItem, object>> keySelector = sortBy?.ToLowerInvariant() switch
        {
            "price" => x => x.Price,
            "rating" => x => x.AvgRating ?? 0,
            _ => x => x.Name,
        };

        return descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }

    private static void SyncAllergens(MenuItem entity, List<int> allergenIds) =>
        CollectionSyncHelper.Sync(
            entity.MenuItemAllergens,
            allergenIds,
            x => x.AllergenId,
            allergenId => new MenuItemAllergen { AllergenId = allergenId });

    private static void SyncDietaryTags(MenuItem entity, List<int> dietaryTagIds) =>
        CollectionSyncHelper.Sync(
            entity.MenuItemDietaryTags,
            dietaryTagIds,
            x => x.DietaryTagId,
            dietaryTagId => new MenuItemDietaryTag { DietaryTagId = dietaryTagId });

    private async Task EnsureMenuItemExistsAsync(int id, CancellationToken cancellationToken)
    {
        var exists = await dbContext.MenuItems.AnyAsync(x => x.Id == id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Jelo sa Id {id} ne postoji.");
        }
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.MenuCategories.AnyAsync(x => x.Id == categoryId, cancellationToken);
        if (!exists)
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["MenuCategoryId"] = [$"Kategorija sa Id {categoryId} ne postoji."],
            });
        }
    }

    private async Task EnsureReferencesExistAsync(List<int> allergenIds, List<int> dietaryTagIds, CancellationToken cancellationToken)
    {
        if (allergenIds.Count > 0)
        {
            var count = await dbContext.Allergens.CountAsync(x => allergenIds.Contains(x.Id), cancellationToken);
            if (count != allergenIds.Distinct().Count())
            {
                throw new Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["AllergenIds"] = ["Jedan ili više navedenih alergena ne postoji."],
                });
            }
        }

        if (dietaryTagIds.Count > 0)
        {
            var count = await dbContext.DietaryTags.CountAsync(x => dietaryTagIds.Contains(x.Id), cancellationToken);
            if (count != dietaryTagIds.Distinct().Count())
            {
                throw new Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["DietaryTagIds"] = ["Jedna ili više navedenih oznaka ne postoji."],
                });
            }
        }
    }
}
