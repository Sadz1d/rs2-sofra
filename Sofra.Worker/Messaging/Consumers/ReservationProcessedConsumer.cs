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

public class ReservationProcessedConsumer(
    RabbitMqConnectionService connectionService,
    IOptions<RabbitMqOptions> rabbitOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationProcessedConsumer> logger)
    : EventConsumerBase<ReservationProcessedEvent>(connectionService, rabbitOptions, scopeFactory, logger, "sofra.reservation-processed", EventRoutingKeys.ReservationProcessed)
{
    protected override async Task HandleAsync(ReservationProcessedEvent @event, IServiceProvider services, CancellationToken cancellationToken)
    {
        var emailSender = services.GetRequiredService<IEmailSender>();
        var dbContext = services.GetRequiredService<WorkerDbContext>();

        await emailSender.SendAsync(@event.UserEmail, @event.UserName, "Sofra - vaša rezervacija", BuildBody(@event), cancellationToken);

        dbContext.Notifications.Add(new Notification
        {
            UserId = @event.UserId,
            Type = NotificationType.Reservation,
            Title = "Rezervacija",
            Text = $"Vaša rezervacija za {@event.ReservationAt:dd.MM.yyyy. HH:mm} je \"{@event.NewStatus}\".",
            ReferenceId = @event.ReservationId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        });
    }

    // NewStatus dolazi kao gotov bosanski naziv iz ReservationStateMachine.GetStatusLabel - mora ostati usklađeno.
    private static string BuildBody(ReservationProcessedEvent @event)
    {
        var when = @event.ReservationAt.ToString("dd.MM.yyyy. 'u' HH:mm");

        return @event.NewStatus switch
        {
            "Potvrđena" => $"""
                <p>Poštovani/a {@event.UserName},</p>
                <p>Vaša rezervacija za <strong>{when}</strong> je potvrđena. Radujemo se vašoj posjeti!</p>
                """,
            "Odbijena" => $"""
                <p>Poštovani/a {@event.UserName},</p>
                <p>Nažalost, vaša rezervacija za <strong>{when}</strong> je odbijena.</p>
                {(string.IsNullOrWhiteSpace(@event.RejectReason) ? string.Empty : $"<p>Razlog: {@event.RejectReason}</p>")}
                {(@event.AlternativeAt.HasValue ? $"<p>Predlažemo alternativni termin: <strong>{@event.AlternativeAt:dd.MM.yyyy. HH:mm}</strong>.</p>" : string.Empty)}
                """,
            _ => $"""
                <p>Poštovani/a {@event.UserName},</p>
                <p>Status vaše rezervacije za <strong>{when}</strong> je promijenjen u "{@event.NewStatus}".</p>
                """,
        };
    }
}
