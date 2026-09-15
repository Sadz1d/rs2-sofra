using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sofra.Shared.Events;
using Sofra.Worker.Options;
using Sofra.Worker.Services.Interfaces;

namespace Sofra.Worker.Messaging.Consumers;

public class PasswordResetRequestedConsumer(
    RabbitMqConnectionService connectionService,
    IOptions<RabbitMqOptions> rabbitOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<PasswordResetRequestedConsumer> logger)
    : EventConsumerBase<PasswordResetRequestedEvent>(connectionService, rabbitOptions, scopeFactory, logger, "sofra.password-reset-requested", EventRoutingKeys.PasswordResetRequested)
{
    // Namjerno bez Notification zapisa - korisnik u ovom trenutku nije prijavljen, samo e-mail ima smisla.
    protected override async Task HandleAsync(PasswordResetRequestedEvent @event, IServiceProvider services, CancellationToken cancellationToken)
    {
        var emailSender = services.GetRequiredService<IEmailSender>();

        var body = $"""
            <p>Poštovani/a {@event.UserName},</p>
            <p>Zatražili ste reset lozinke. Vaš kod je:</p>
            <p style="font-size:24px;font-weight:bold;letter-spacing:4px;">{@event.ResetCode}</p>
            <p>Kod vrijedi do {@event.ExpiresAt:dd.MM.yyyy. HH:mm} i može se iskoristiti samo jednom.</p>
            <p>Ako niste vi zatražili reset lozinke, slobodno zanemarite ovaj e-mail.</p>
            """;

        await emailSender.SendAsync(@event.UserEmail, @event.UserName, "Sofra - reset lozinke", body, cancellationToken);
    }
}
