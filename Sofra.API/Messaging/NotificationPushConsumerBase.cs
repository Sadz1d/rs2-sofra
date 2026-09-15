using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Sofra.API.Options;
using Sofra.Shared.Events;

namespace Sofra.API.Messaging;

/// <summary>
/// API-ova SOPSTVENA (druga) konzumacija istih dogadjaja koje vec obradjuje Worker - RabbitMQ topic
/// exchange isporucuje po jednu kopiju poruke svakom queue-u vezanom na taj routing key, pa ovo ne
/// utice na Workerov queue/retry/dead-letter. Razlog: Worker (zaseban kontejner, bez SignalR konteksta
/// i bez backplane-a) ne moze sam gurnuti poruku u hub; upravo je persistentni Notification red koji
/// Worker upise "izvor istine", pa API ovdje samo simulira isti prikazni sadrzaj i salje ga uzivo preko
/// NotificationHub-a. Zato je namjerno MNOGO jednostavniji od Workerovog EventConsumerBase-a: nema
/// retry/dead-letter/idempotentnost, jer promasen live-push nije gubitak podataka - korisnik ce ga
/// svakako vidjeti kroz GET /api/notifications; poruka se uvijek ack-uje.
/// </summary>
public abstract class NotificationPushConsumerBase<TEvent>(
    RabbitMqConnectionService connectionService,
    IOptions<RabbitMqOptions> rabbitOptions,
    IServiceScopeFactory scopeFactory,
    ILogger logger,
    string queueName,
    string routingKey) : BackgroundService
    where TEvent : class, IEvent
{
    protected abstract Task PushAsync(TEvent @event, IServiceProvider scopedServices, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = connectionService.Connection;
        if (connection is null)
        {
            logger.LogWarning(
                "RabbitMQ nije dostupan pri startu - live-push za {EventType} neće raditi dok se API ne restartuje uz dostupan RabbitMQ.",
                typeof(TEvent).Name);
            return;
        }

        var exchange = rabbitOptions.Value.Exchange;
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(queueName, exchange, routingKey, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                var @event = JsonSerializer.Deserialize<TEvent>(json);
                if (@event is not null)
                {
                    using var scope = scopeFactory.CreateScope();
                    await PushAsync(@event, scope.ServiceProvider, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Greška pri live-push obradi događaja {EventType} iz reda {Queue}.", typeof(TEvent).Name, queueName);
            }
            finally
            {
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, stoppingToken);

        logger.LogInformation("Live-push consumer za {EventType} sluša red {Queue}.", typeof(TEvent).Name, queueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normalno gasenje servisa.
        }
    }
}
