using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sofra.API.Hubs;
using Sofra.API.Hubs.Messages;
using Sofra.API.Options;
using Sofra.Shared.Events;

namespace Sofra.API.Messaging.Consumers;

/// <summary>Isti prikazni sadrzaj kao PaymentSucceededConsumer u Workeru (koji upisuje Notification red) - mora ostati usklađeno.</summary>
public class PaymentSucceededNotifyConsumer(
    RabbitMqConnectionService connectionService,
    IOptions<RabbitMqOptions> rabbitOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentSucceededNotifyConsumer> logger)
    : NotificationPushConsumerBase<PaymentSucceededEvent>(connectionService, rabbitOptions, scopeFactory, logger, "sofra.api-notify.payment-succeeded", EventRoutingKeys.PaymentSucceeded)
{
    protected override async Task PushAsync(PaymentSucceededEvent @event, IServiceProvider services, CancellationToken cancellationToken)
    {
        var hub = services.GetRequiredService<IHubContext<NotificationHub>>();

        var message = new NotificationPushMessage(
            $"Plaćanje primljeno - {@event.OrderNumber}",
            $"Vaše plaćanje od {@event.Amount:0.00} KM za narudžbu {@event.OrderNumber} je uspješno primljeno.",
            "Payment",
            @event.OrderId,
            @event.OccurredAt);

        await hub.Clients.Group($"user:{@event.UserId}").SendAsync("notificationReceived", message, cancellationToken);
    }
}
