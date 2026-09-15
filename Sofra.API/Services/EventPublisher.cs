using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Sofra.API.Messaging;
using Sofra.API.Options;
using Sofra.API.Services.Interfaces;
using Sofra.Shared.Events;

namespace Sofra.API.Services;

public class EventPublisher(
    RabbitMqConnectionService connectionService,
    IOptions<RabbitMqOptions> options,
    ILogger<EventPublisher> logger) : IEventPublisher
{
    private readonly RabbitMqOptions _options = options.Value;

    public async Task PublishAsync<TEvent>(TEvent @event, string routingKey, CancellationToken cancellationToken = default) where TEvent : class, IEvent
    {
        var connection = connectionService.Connection;
        if (connection is null)
        {
            logger.LogWarning("RabbitMQ konekcija nije dostupna - događaj {EventType} ({RoutingKey}, EventId={EventId}) nije objavljen.",
                typeof(TEvent).Name, routingKey, @event.EventId);
            return;
        }

        try
        {
            using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

            var body = JsonSerializer.SerializeToUtf8Bytes(@event);
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
            };

            await channel.BasicPublishAsync(_options.Exchange, routingKey, mandatory: false, properties, body, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Greška pri objavljivanju događaja {EventType} ({RoutingKey}, EventId={EventId}) na RabbitMQ.",
                typeof(TEvent).Name, routingKey, @event.EventId);
        }
    }
}
