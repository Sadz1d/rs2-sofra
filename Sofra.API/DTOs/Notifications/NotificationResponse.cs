using Sofra.API.Enums;

namespace Sofra.API.DTOs.Notifications;

public record NotificationResponse(
    int Id, string Title, string Text, NotificationType Type, int? ReferenceId, bool IsRead, DateTime CreatedAt);
