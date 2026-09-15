using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sofra.API.Hubs;
using Sofra.API.Hubs.Messages;
using Sofra.API.Options;
using Sofra.Shared.Events;

namespace Sofra.API.Messaging.Consumers;

/// <summary>Isti prikazni sadrzaj kao ReservationProcessedConsumer u Workeru (koji upisuje Notification red) - mora ostati usklađeno.</summary>
public class ReservationProcessedNotifyConsumer(
    RabbitMqConnectionService connectionService,
    IOptions<RabbitMqOptions> rabbitOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationProcessedNotifyConsumer> logger)
    : NotificationPushConsumerBase<ReservationProcessedEvent>(connectionService, rabbitOptions, scopeFactory, logger, "sofra.api-notify.reservation-processed", EventRoutingKeys.ReservationProcessed)
{
    protected override async Task PushAsync(ReservationProcessedEvent @event, IServiceProvider services, CancellationToken cancellationToken)
    {
        var hub = services.GetRequiredService<IHubContext<NotificationHub>>();

        var message = new NotificationPushMessage(
            "Rezervacija",
            $"Vaša rezervacija za {@event.ReservationAt:dd.MM.yyyy. HH:mm} je \"{@event.NewStatus}\".",
            "Reservation",
            @event.ReservationId,
            @event.OccurredAt);

        await hub.Clients.Group($"user:{@event.UserId}").SendAsync("notificationReceived", message, cancellationToken);
    }
}
