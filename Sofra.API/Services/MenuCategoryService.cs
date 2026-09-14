using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Catalog;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class MenuCategoryService(AppDbContext dbContext) : IMenuCategoryService
{
    public async Task<PagedResult<MenuCategoryResponse>> GetListAsync(MenuCategoryListRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.MenuCategories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.Name.Contains(request.Search));
        }

        query = ApplySort(query, request.SortBy, request.SortDesc);

        var responseQuery = query.Select(x => new MenuCategoryResponse(x.Id, x.Name, x.Description, x.SortOrder, x.ImageUrl, x.IsActive));

        return await responseQuery.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<MenuCategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.MenuCategories.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MenuCategoryResponse(x.Id, x.Name, x.Description, x.SortOrder, x.ImageUrl, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return category ?? throw new NotFoundException($"Kategorija jela sa Id {id} ne postoji.");
    }

    public async Task<MenuCategoryResponse> CreateAsync(MenuCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureUniqueNameAsync(request.Name, excludeId: null, cancellationToken);

        var category = new MenuCategory
        {
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            ImageUrl = request.ImageUrl,
            IsActive = request.IsActive,
        };

        dbContext.MenuCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MenuCategoryResponse(category.Id, category.Name, category.Description, category.SortOrder, category.ImageUrl, category.IsActive);
    }

    public async Task<MenuCategoryResponse> UpdateAsync(int id, MenuCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.MenuCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Kategorija jela sa Id {id} ne postoji.");

        await EnsureUniqueNameAsync(request.Name, excludeId: id, cancellationToken);

        category.Name = request.Name;
        category.Description = request.Description;
        category.SortOrder = request.SortOrder;
        category.ImageUrl = request.ImageUrl;
        category.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new MenuCategoryResponse(category.Id, category.Name, category.Description, category.SortOrder, category.ImageUrl, category.IsActive);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.MenuCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Kategorija jela sa Id {id} ne postoji.");

        var usageCount = await dbContext.MenuItems.CountAsync(x => x.MenuCategoryId == id, cancellationToken);
        if (usageCount > 0)
        {
            var noun = BosnianPluralizer.Pluralize(usageCount, "jelo", "jela", "jela");
            throw new BusinessException($"Kategorija '{category.Name}' se ne može obrisati jer je koristi {usageCount} {noun}.");
        }

        dbContext.MenuCategories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUniqueNameAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.MenuCategories
            .AnyAsync(x => x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);

        if (exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Name"] = [$"Kategorija sa nazivom '{name}' već postoji."],
            });
        }
    }

    private static IQueryable<MenuCategory> ApplySort(IQueryable<MenuCategory> query, string? sortBy, bool descending)
    {
        Expression<Func<MenuCategory, object>> keySelector = sortBy?.ToLowerInvariant() switch
        {
            "sortorder" => x => x.SortOrder,
            "isactive" => x => x.IsActive,
            _ => x => x.Name,
        };

        return descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }
}
