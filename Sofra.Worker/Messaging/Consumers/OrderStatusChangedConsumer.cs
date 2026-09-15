using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sofra.Shared.Events;
using Sofra.Worker.Data;
using Sofra.Worker.Entities;
using Sofra.Worker.Enums;
using Sofra.Worker.Options;
using Sofra.Worker.Services.Interfaces;

namespace Sofra.Worker.Messaging.Consumers;

public class OrderStatusChangedConsumer(
    RabbitMqConnectionService connectionService,
    IOptions<RabbitMqOptions> rabbitOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderStatusChangedConsumer> logger)
    : EventConsumerBase<OrderStatusChangedEvent>(connectionService, rabbitOptions, scopeFactory, logger, "sofra.order-status-changed", EventRoutingKeys.OrderStatusChanged)
{
    protected override async Task HandleAsync(OrderStatusChangedEvent @event, IServiceProvider services, CancellationToken cancellationToken)
    {
        var emailSender = services.GetRequiredService<IEmailSender>();
        var dbContext = services.GetRequiredService<WorkerDbContext>();

        var subject = $"Sofra - narudžba {@event.OrderNumber}";
        var reasonLine = string.IsNullOrWhiteSpace(@event.CancelReason) ? string.Empty : $"<p>Razlog: {@event.CancelReason}</p>";
        var body = $"""
            <p>Poštovani/a {@event.UserName},</p>
            <p>Status vaše narudžbe <strong>{@event.OrderNumber}</strong> je promijenjen iz "{@event.OldStatus}" u "<strong>{@event.NewStatus}</strong>".</p>
            {reasonLine}
            <p>Hvala što ste izabrali Sofra.</p>
            """;

        await emailSender.SendAsync(@event.UserEmail, @event.UserName, subject, body, cancellationToken);

        dbContext.Notifications.Add(new Notification
        {
            UserId = @event.UserId,
            Type = NotificationType.OrderStatus,
            Title = $"Narudžba {@event.OrderNumber}",
            Text = $"Status narudžbe je promijenjen u \"{@event.NewStatus}\".",
            ReferenceId = @event.OrderId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        });
    }
}
