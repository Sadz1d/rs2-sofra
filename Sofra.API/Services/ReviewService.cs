using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Reviews;
using Sofra.API.Entities;
using Sofra.API.Enums;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Reviews;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class ReviewService(AppDbContext dbContext) : IReviewService
{
    public async Task<PagedResult<ReviewResponse>> GetListAsync(ReviewListRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Reviews.AsNoTracking().AsQueryable();

        if (request.MenuItemId.HasValue)
        {
            query = query.Where(x => x.MenuItemId == request.MenuItemId);
        }

        if (request.Rating.HasValue)
        {
            query = query.Where(x => x.Rating == request.Rating);
        }

        if (request.HasReply.HasValue)
        {
            query = request.HasReply.Value ? query.Where(x => x.Reply != null) : query.Where(x => x.Reply == null);
        }

        query = request.SortDesc ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt);

        return await query.Select(ProjectToResponse).ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<ReviewResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await dbContext.Reviews.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(ProjectToResponse)
            .FirstOrDefaultAsync(cancellationToken);

        return response ?? throw new NotFoundException($"Recenzija sa Id {id} ne postoji.");
    }

    public async Task<ReviewResponse> CreateAsync(ReviewRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken)
            ?? throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["OrderId"] = [$"Narudžba sa Id {request.OrderId} ne postoji."],
            });

        if (order.UserId != actorUserId)
        {
            throw new ForbiddenException("Ne možete ocijeniti tuđu narudžbu.");
        }

        if (order.Status != OrderStatus.Completed)
        {
            throw new BusinessException("Jelo se može ocijeniti tek kada je narudžba završena.");
        }

        if (order.Items.All(x => x.MenuItemId != request.MenuItemId))
        {
            throw new BusinessException("Odabrano jelo nije dio ove narudžbe.");
        }

        var alreadyReviewed = await dbContext.Reviews.AnyAsync(x =>
            x.OrderId == request.OrderId && x.UserId == actorUserId && x.MenuItemId == request.MenuItemId, cancellationToken);
        if (alreadyReviewed)
        {
            throw new BusinessException("Već ste ocijenili ovo jelo iz ove narudžbe.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var review = new Review
        {
            CreatedAt = DateTime.UtcNow,
            OrderId = request.OrderId,
            UserId = actorUserId,
            MenuItemId = request.MenuItemId,
            Rating = request.Rating,
            Comment = request.Comment,
        };

        dbContext.Reviews.Add(review);
        await dbContext.SaveChangesAsync(cancellationToken);

        await RecalculateMenuItemStatsAsync(request.MenuItemId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(review.Id, cancellationToken);
    }

    public async Task<ReviewResponse> UpdateAsync(int id, ReviewUpdateRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var review = await dbContext.Reviews.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Recenzija sa Id {id} ne postoji.");

        if (review.UserId != actorUserId)
        {
            throw new ForbiddenException("Ne možete izmijeniti tuđu recenziju.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        review.Rating = request.Rating;
        review.Comment = request.Comment;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (review.MenuItemId.HasValue)
        {
            await RecalculateMenuItemStatsAsync(review.MenuItemId.Value, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, int actorUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var review = await dbContext.Reviews.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Recenzija sa Id {id} ne postoji.");

        if (!isStaff && review.UserId != actorUserId)
        {
            throw new ForbiddenException("Ne možete obrisati tuđu recenziju.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var menuItemId = review.MenuItemId;
        dbContext.Reviews.Remove(review);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (menuItemId.HasValue)
        {
            await RecalculateMenuItemStatsAsync(menuItemId.Value, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ReviewResponse> ReplyAsync(int id, ReviewReplyRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var review = await dbContext.Reviews.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Recenzija sa Id {id} ne postoji.");

        review.Reply = request.Reply;
        review.RepliedById = actorUserId;
        review.RepliedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task RecalculateMenuItemStatsAsync(int menuItemId, CancellationToken cancellationToken)
    {
        var stats = await dbContext.Reviews
            .Where(x => x.MenuItemId == menuItemId)
            .GroupBy(x => x.MenuItemId)
            .Select(g => new { Count = g.Count(), Avg = g.Average(x => (decimal)x.Rating) })
            .FirstOrDefaultAsync(cancellationToken);

        var menuItem = await dbContext.MenuItems.FirstAsync(x => x.Id == menuItemId, cancellationToken);
        menuItem.ReviewCount = stats?.Count ?? 0;
        menuItem.AvgRating = stats?.Avg;
    }

    private static readonly System.Linq.Expressions.Expression<Func<Review, ReviewResponse>> ProjectToResponse = x => new ReviewResponse(
        x.Id, x.OrderId, x.UserId, x.User.FirstName + " " + x.User.LastName,
        x.MenuItemId, x.MenuItem == null ? null : x.MenuItem.Name,
        x.Rating, x.Comment,
        x.Reply, x.RepliedById, x.RepliedBy == null ? null : x.RepliedBy.FirstName + " " + x.RepliedBy.LastName, x.RepliedAt,
        x.CreatedAt);
}
