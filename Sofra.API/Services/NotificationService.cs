using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Notifications;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Notifications;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class NotificationService(AppDbContext dbContext) : INotificationService
{
    public async Task<PagedResult<NotificationResponse>> GetListAsync(NotificationListRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notifications.AsNoTracking().Where(x => x.UserId == userId);

        if (request.IsRead.HasValue)
        {
            query = query.Where(x => x.IsRead == request.IsRead);
        }

        // Notifikacije se uvijek prikazuju najnovije prvo - nema smislene alternative za "zvono" listu.
        var projected = query.OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationResponse(x.Id, x.Title, x.Text, x.Type, x.ReferenceId, x.IsRead, x.CreatedAt));

        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task MarkReadAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException($"Notifikacija sa Id {id} ne postoji.");

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        await dbContext.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsRead, true), cancellationToken);
    }
}
