namespace Sofra.API.Hubs.Messages;

/// <summary>Payload za NotificationHub "notificationReceived" - isti oblik sadrzaja kao Notification red koji upisuje Worker.</summary>
public record NotificationPushMessage(string Title, string Text, string Type, int? ReferenceId, DateTime CreatedAt);
