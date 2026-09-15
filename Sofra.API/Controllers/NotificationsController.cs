using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Notifications;
using Sofra.API.Extensions;
using Sofra.API.Requests.Notifications;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationResponse>>> GetList([FromQuery] NotificationListRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetListAsync(request, User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        await service.MarkReadAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await service.MarkAllReadAsync(User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
