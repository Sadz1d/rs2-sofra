using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Promotions;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Promotions;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class PromotionService(AppDbContext dbContext) : IPromotionService
{
    private static readonly Expression<Func<Promotion, PromotionResponse>> ProjectToResponse = x => new PromotionResponse(
        x.Id, x.Name, x.Code,
        x.DiscountType, x.Value, x.Scope,
        x.MenuCategoryId, x.MenuCategory == null ? null : x.MenuCategory.Name,
        x.ValidFrom, x.ValidTo,
        x.MaxUses, x.UsedCount, x.MinOrderAmount,
        x.IsActive);

    public async Task<PagedResult<PromotionResponse>> GetListAsync(PromotionListRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Promotions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.Name.Contains(request.Search) || x.Code.Contains(request.Search));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive);
        }

        if (request.Scope.HasValue)
        {
            query = query.Where(x => x.Scope == request.Scope);
        }

        if (request.IsCurrentlyValid == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(x => x.IsActive && x.ValidFrom <= now && x.ValidTo >= now && (x.MaxUses == null || x.UsedCount < x.MaxUses));
        }

        query = request.SortDesc ? query.OrderByDescending(x => x.ValidFrom) : query.OrderBy(x => x.ValidFrom);

        return await query.Select(ProjectToResponse).ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<PromotionResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await dbContext.Promotions.AsNoTracking().Where(x => x.Id == id).Select(ProjectToResponse).FirstOrDefaultAsync(cancellationToken);
        return response ?? throw new NotFoundException($"Promocija sa Id {id} ne postoji.");
    }

    public async Task<PromotionResponse> CreateAsync(PromotionRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCodeUniqueAsync(request.Code, excludeId: null, cancellationToken);
        await EnsureCategoryExistsAsync(request.MenuCategoryId, cancellationToken);

        var entity = new Promotion
        {
            Name = request.Name,
            Code = request.Code,
            DiscountType = request.DiscountType,
            Value = request.Value,
            Scope = request.Scope,
            MenuCategoryId = request.MenuCategoryId,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            MaxUses = request.MaxUses,
            MinOrderAmount = request.MinOrderAmount,
            IsActive = request.IsActive,
            UsedCount = 0,
        };

        dbContext.Promotions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<PromotionResponse> UpdateAsync(int id, PromotionRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Promotions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Promocija sa Id {id} ne postoji.");

        await EnsureCodeUniqueAsync(request.Code, excludeId: id, cancellationToken);
        await EnsureCategoryExistsAsync(request.MenuCategoryId, cancellationToken);

        entity.Name = request.Name;
        entity.Code = request.Code;
        entity.DiscountType = request.DiscountType;
        entity.Value = request.Value;
        entity.Scope = request.Scope;
        entity.MenuCategoryId = request.MenuCategoryId;
        entity.ValidFrom = request.ValidFrom;
        entity.ValidTo = request.ValidTo;
        entity.MaxUses = request.MaxUses;
        entity.MinOrderAmount = request.MinOrderAmount;
        entity.IsActive = request.IsActive;
        // UsedCount se nikad ne mijenja kroz CRUD - jedino ga inkrementira OrderService pri kreiranju narudzbe.

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Promotions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Promocija sa Id {id} ne postoji.");

        // Soft delete (IsActive) - Order.PromotionId je Restrict pa hard delete puca na historijskim narudzbama.
        entity.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCodeUniqueAsync(string code, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Promotions.AnyAsync(x => x.Code == code && (excludeId == null || x.Id != excludeId), cancellationToken);
        if (exists)
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["Code"] = [$"Promo kod '{code}' je već u upotrebi."],
            });
        }
    }

    private async Task EnsureCategoryExistsAsync(int? menuCategoryId, CancellationToken cancellationToken)
    {
        if (menuCategoryId.HasValue && !await dbContext.MenuCategories.AnyAsync(x => x.Id == menuCategoryId, cancellationToken))
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["MenuCategoryId"] = [$"Kategorija sa Id {menuCategoryId} ne postoji."],
            });
        }
    }
}
