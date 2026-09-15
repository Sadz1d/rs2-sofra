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

public class PaymentSucceededConsumer(
    RabbitMqConnectionService connectionService,
    IOptions<RabbitMqOptions> rabbitOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentSucceededConsumer> logger)
    : EventConsumerBase<PaymentSucceededEvent>(connectionService, rabbitOptions, scopeFactory, logger, "sofra.payment-succeeded", EventRoutingKeys.PaymentSucceeded)
{
    protected override async Task HandleAsync(PaymentSucceededEvent @event, IServiceProvider services, CancellationToken cancellationToken)
    {
        var emailSender = services.GetRequiredService<IEmailSender>();
        var dbContext = services.GetRequiredService<WorkerDbContext>();

        var subject = $"Sofra - potvrda plaćanja za narudžbu {@event.OrderNumber}";
        var body = $"""
            <p>Poštovani/a {@event.UserName},</p>
            <p>Vaše plaćanje za narudžbu <strong>{@event.OrderNumber}</strong> u iznosu od <strong>{@event.Amount:0.00} KM</strong> je uspješno primljeno.</p>
            <p>Hvala što ste izabrali Sofra.</p>
            """;

        await emailSender.SendAsync(@event.UserEmail, @event.UserName, subject, body, cancellationToken);

        dbContext.Notifications.Add(new Notification
        {
            UserId = @event.UserId,
            Type = NotificationType.Payment,
            Title = $"Plaćanje primljeno - {@event.OrderNumber}",
            Text = $"Vaše plaćanje od {@event.Amount:0.00} KM za narudžbu {@event.OrderNumber} je uspješno primljeno.",
            ReferenceId = @event.OrderId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        });
    }
}
