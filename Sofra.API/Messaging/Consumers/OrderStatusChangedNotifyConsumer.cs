using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sofra.API.Hubs;
using Sofra.API.Hubs.Messages;
using Sofra.API.Options;
using Sofra.Shared.Events;

namespace Sofra.API.Messaging.Consumers;

/// <summary>Isti prikazni sadrzaj kao OrderStatusChangedConsumer u Workeru (koji upisuje Notification red) - mora ostati usklađeno.</summary>
public class OrderStatusChangedNotifyConsumer(
    RabbitMqConnectionService connectionService,
    IOptions<RabbitMqOptions> rabbitOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderStatusChangedNotifyConsumer> logger)
    : NotificationPushConsumerBase<OrderStatusChangedEvent>(connectionService, rabbitOptions, scopeFactory, logger, "sofra.api-notify.order-status-changed", EventRoutingKeys.OrderStatusChanged)
{
    protected override async Task PushAsync(OrderStatusChangedEvent @event, IServiceProvider services, CancellationToken cancellationToken)
    {
        var hub = services.GetRequiredService<IHubContext<NotificationHub>>();

        var message = new NotificationPushMessage(
            $"Narudžba {@event.OrderNumber}",
            $"Status narudžbe je promijenjen u \"{@event.NewStatus}\".",
            "OrderStatus",
            @event.OrderId,
            @event.OccurredAt);

        await hub.Clients.Group($"user:{@event.UserId}").SendAsync("notificationReceived", message, cancellationToken);
    }
}
