using Microsoft.AspNetCore.Http;
using Sofra.API.DTOs;
using Sofra.API.DTOs.News;
using Sofra.API.Requests.News;

namespace Sofra.API.Services.Interfaces;

public interface INewsService
{
    Task<PagedResult<NewsResponse>> GetListAsync(NewsListRequest request, bool isStaff, CancellationToken cancellationToken = default);
    Task<NewsResponse> GetByIdAsync(int id, bool isStaff, CancellationToken cancellationToken = default);
    Task<NewsResponse> CreateAsync(NewsRequest request, int actorUserId, CancellationToken cancellationToken = default);
    Task<NewsResponse> UpdateAsync(int id, NewsRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<NewsResponse> SetImageAsync(int id, IFormFile file, CancellationToken cancellationToken = default);
}
