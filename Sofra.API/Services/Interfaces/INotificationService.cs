using Sofra.API.DTOs;
using Sofra.API.DTOs.Notifications;
using Sofra.API.Requests.Notifications;

namespace Sofra.API.Services.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationResponse>> GetListAsync(NotificationListRequest request, int userId, CancellationToken cancellationToken = default);
    Task MarkReadAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default);
}
