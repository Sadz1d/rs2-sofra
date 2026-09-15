using Sofra.API.DTOs;
using Sofra.API.DTOs.Reviews;
using Sofra.API.Requests.Reviews;

namespace Sofra.API.Services.Interfaces;

public interface IReviewService
{
    Task<PagedResult<ReviewResponse>> GetListAsync(ReviewListRequest request, CancellationToken cancellationToken = default);
    Task<ReviewResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ReviewResponse> CreateAsync(ReviewRequest request, int actorUserId, CancellationToken cancellationToken = default);
    Task<ReviewResponse> UpdateAsync(int id, ReviewUpdateRequest request, int actorUserId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, int actorUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task<ReviewResponse> ReplyAsync(int id, ReviewReplyRequest request, int actorUserId, CancellationToken cancellationToken = default);
}
