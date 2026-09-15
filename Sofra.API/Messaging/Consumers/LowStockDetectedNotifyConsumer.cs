using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sofra.API.Hubs;
using Sofra.API.Hubs.Messages;
using Sofra.API.Options;
using Sofra.Shared.Events;

namespace Sofra.API.Messaging.Consumers;

/// <summary>Isti prikazni sadrzaj kao LowStockDetectedConsumer u Workeru (koji upisuje Notification red za svakog Admina) - mora ostati usklađeno.</summary>
public class LowStockDetectedNotifyConsumer(
    RabbitMqConnectionService connectionService,
    IOptions<RabbitMqOptions> rabbitOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<LowStockDetectedNotifyConsumer> logger)
    : NotificationPushConsumerBase<LowStockDetectedEvent>(connectionService, rabbitOptions, scopeFactory, logger, "sofra.api-notify.low-stock-detected", EventRoutingKeys.LowStockDetected)
{
    protected override async Task PushAsync(LowStockDetectedEvent @event, IServiceProvider services, CancellationToken cancellationToken)
    {
        var hub = services.GetRequiredService<IHubContext<NotificationHub>>();

        var message = new NotificationPushMessage(
            "Niske zalihe",
            $"Stanje namirnice '{@event.InventoryItemName}' je {@event.Quantity} {@event.UnitAbbreviation} (minimum {@event.MinQuantity} {@event.UnitAbbreviation}).",
            "LowStock",
            @event.InventoryItemId,
            @event.OccurredAt);

        foreach (var adminUserId in @event.AdminUserIds)
        {
            await hub.Clients.Group($"user:{adminUserId}").SendAsync("notificationReceived", message, cancellationToken);
        }
    }
}
