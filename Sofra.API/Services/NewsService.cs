using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.News;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.News;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class NewsService(AppDbContext dbContext, IImageUploadService imageUploadService) : INewsService
{
    private static readonly Expression<Func<News, NewsResponse>> ProjectToResponse = x => new NewsResponse(
        x.Id, x.Title, x.Text, x.ImageUrl,
        x.PublishAt, x.IsPublished, x.SendPush,
        x.CreatedById, x.CreatedBy.FirstName + " " + x.CreatedBy.LastName);

    public async Task<PagedResult<NewsResponse>> GetListAsync(NewsListRequest request, bool isStaff, CancellationToken cancellationToken = default)
    {
        var query = dbContext.News.AsNoTracking().AsQueryable();

        // Gost/mobilni klijent nikad ne vidi nacrte, bez obzira sta posalje u upitu.
        var isPublishedFilter = isStaff ? request.IsPublished : true;
        if (isPublishedFilter.HasValue)
        {
            query = query.Where(x => x.IsPublished == isPublishedFilter);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.Title.Contains(request.Search) || x.Text.Contains(request.Search));
        }

        query = request.SortDesc ? query.OrderBy(x => x.PublishAt) : query.OrderByDescending(x => x.PublishAt);

        return await query.Select(ProjectToResponse).ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<NewsResponse> GetByIdAsync(int id, bool isStaff, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.News.AsNoTracking().Where(x => x.Id == id).Select(ProjectToResponse).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Obavijest sa Id {id} ne postoji.");

        if (!isStaff && !entity.IsPublished)
        {
            throw new NotFoundException($"Obavijest sa Id {id} ne postoji.");
        }

        return entity;
    }

    public async Task<NewsResponse> CreateAsync(NewsRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var entity = new News
        {
            Title = request.Title,
            Text = request.Text,
            ImageUrl = string.Empty,
            PublishAt = request.PublishAt,
            IsPublished = request.IsPublished,
            SendPush = request.SendPush,
            CreatedById = actorUserId,
        };

        dbContext.News.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, isStaff: true, cancellationToken);
    }

    public async Task<NewsResponse> UpdateAsync(int id, NewsRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.News.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Obavijest sa Id {id} ne postoji.");

        entity.Title = request.Title;
        entity.Text = request.Text;
        entity.PublishAt = request.PublishAt;
        entity.IsPublished = request.IsPublished;
        entity.SendPush = request.SendPush;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, isStaff: true, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.News.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Obavijest sa Id {id} ne postoji.");

        imageUploadService.DeleteIfExists(entity.ImageUrl);

        dbContext.News.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<NewsResponse> SetImageAsync(int id, IFormFile file, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.News.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Obavijest sa Id {id} ne postoji.");

        var newUrl = await imageUploadService.SaveAsync(file, "news", cancellationToken);
        imageUploadService.DeleteIfExists(entity.ImageUrl);
        entity.ImageUrl = newUrl;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, isStaff: true, cancellationToken);
    }
}
