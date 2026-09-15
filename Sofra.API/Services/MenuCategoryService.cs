using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs.Catalog;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests;
using Sofra.API.Requests.Catalog;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class MenuCategoryService(AppDbContext dbContext, IImageUploadService imageUploadService)
    : LookupService<MenuCategory, MenuCategoryResponse, MenuCategoryRequest>(dbContext), IMenuCategoryService
{
    protected override DbSet<MenuCategory> Set => DbContext.MenuCategories;

    protected override string EntityLabel => "Kategorija jela";

    protected override Expression<Func<MenuCategory, MenuCategoryResponse>> ProjectToResponse =>
        x => new MenuCategoryResponse(x.Id, x.Name, x.Description, x.SortOrder, x.ImageUrl, x.IsActive);

    protected override IQueryable<MenuCategory> ApplyExtraFilters(IQueryable<MenuCategory> query, PagedRequest request) =>
        request is MenuCategoryListRequest { IsActive: not null } r ? query.Where(x => x.IsActive == r.IsActive) : query;

    protected override MenuCategory CreateEntity(MenuCategoryRequest request) => new()
    {
        Name = request.Name,
        Description = request.Description,
        SortOrder = request.SortOrder,
        ImageUrl = request.ImageUrl,
        IsActive = request.IsActive,
    };

    protected override void UpdateEntity(MenuCategory entity, MenuCategoryRequest request)
    {
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.SortOrder = request.SortOrder;
        entity.ImageUrl = request.ImageUrl;
        entity.IsActive = request.IsActive;
    }

    protected override async Task EnsureUniqueAsync(MenuCategoryRequest request, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await DbContext.MenuCategories
            .AnyAsync(x => x.Name == request.Name && (excludeId == null || x.Id != excludeId), cancellationToken);

        if (exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Name"] = [$"Kategorija sa nazivom '{request.Name}' već postoji."],
            });
        }
    }

    protected override async Task<LookupUsage> GetUsageAsync(int id, CancellationToken cancellationToken)
    {
        var count = await DbContext.MenuItems.CountAsync(x => x.MenuCategoryId == id, cancellationToken);
        return new LookupUsage(count, "jelo", "jela", "jela");
    }

    public async Task<MenuCategoryResponse> SetImageAsync(int id, IFormFile file, CancellationToken cancellationToken = default)
    {
        var entity = await DbContext.MenuCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage(id));

        var newUrl = await imageUploadService.SaveAsync(file, "menu-categories", cancellationToken);
        imageUploadService.DeleteIfExists(entity.ImageUrl);
        entity.ImageUrl = newUrl;

        await DbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }
}
