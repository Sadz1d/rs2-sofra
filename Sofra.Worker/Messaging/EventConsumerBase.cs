using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Sofra.Shared.Events;
using Sofra.Worker.Data;
using Sofra.Worker.Entities;
using Sofra.Worker.Options;

namespace Sofra.Worker.Messaging;

/// <summary>
/// Zajednicka infrastruktura za sve event consumere: zaseban queue vezan na sofra.events (topic exchange)
/// preko datog routing key-a, manual ack, idempotentna obrada preko ProcessedEvents (EventId), retry sa
/// eksponencijalnim backoff-om (1-2-4-8s) unutar iste poruke, a nakon iscrpljenih pokusaja poruka ide u
/// dead-letter queue (ne u beskonacnu RabbitMQ redelivery petlju - dead-letter je eksplicitan publish + ack).
/// Izvedene klase samo obradjuju konkretan dogadjaj (HandleAsync) - ne pozivaju SaveChangesAsync same,
/// to radi baza klasa u istom SaveChanges pozivu kao i upis u ProcessedEvents (atomicno).
///
/// Prefetch je veci od 1 i obrada poruka je konkurentna (ogranicena na PrefetchCount istovremeno) -
/// consumer.ReceivedAsync samo zauzme slot i odmah vrati kontrolu dispatcheru, obrada ide u pozadini.
/// Da retry-backoff jedne poruke (do 15s) ne blokira sve ostale poruke u redu kao kad bi obrada bila
/// inline-await u dispatch petlji sa prefetch=1. Sami RabbitMQ I/O pozivi (ack/publish na dead-letter)
/// se serijalizuju preko posebnog zakljucavanja jer isti IChannel ne smije primati konkurentne pozive.
/// </summary>
public abstract class EventConsumerBase<TEvent>(
    RabbitMqConnectionService connectionService,
    IOptions<RabbitMqOptions> rabbitOptions,
    IServiceScopeFactory scopeFactory,
    ILogger logger,
    string queueName,
    string routingKey) : BackgroundService
    where TEvent : class, IEvent
{
    private const ushort PrefetchCount = 8;

    private static readonly int[] BackoffSeconds = [1, 2, 4, 8];

    private readonly SemaphoreSlim _concurrencyLimiter = new(PrefetchCount, PrefetchCount);
    private readonly SemaphoreSlim _channelLock = new(1, 1);

    /// <summary>Obradi dogadjaj koristeci servise iz datog scope-a. NE poziva SaveChangesAsync - to radi baza klasa.</summary>
    protected abstract Task HandleAsync(TEvent @event, IServiceProvider scopedServices, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = connectionService.Connection
            ?? throw new InvalidOperationException("RabbitMQ konekcija nije uspostavljena.");

        var exchange = rabbitOptions.Value.Exchange;
        var deadLetterQueue = $"{queueName}.dlq";

        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(queueName, exchange, routingKey, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            // Zauzmi slot pa odmah vrati kontrolu dispatcheru - obrada (ukljucujuci retry-backoff) ide
            // konkurentno u pozadini, ne blokira preuzimanje sljedece poruke iz reda.
            await _concurrencyLimiter.WaitAsync(stoppingToken);
            _ = ProcessDeliveryAsync(channel, deadLetterQueue, ea, stoppingToken);
        };

        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, stoppingToken);

        logger.LogInformation(
            "Consumer za {EventType} sluša red {Queue} (routing key {RoutingKey}, prefetch {Prefetch}).",
            typeof(TEvent).Name, queueName, routingKey, PrefetchCount);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normalno gasenje servisa.
        }
    }

    private async Task ProcessDeliveryAsync(IChannel channel, string deadLetterQueue, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            await OnMessageAsync(channel, deadLetterQueue, ea, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Neuhvaćena greška pri obradi poruke iz reda {Queue}.", queueName);
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    private async Task OnMessageAsync(IChannel channel, string deadLetterQueue, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        async Task AckAsync()
        {
            await _channelLock.WaitAsync(stoppingToken);
            try
            {
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            finally
            {
                _channelLock.Release();
            }
        }

        async Task DeadLetterAndAckAsync()
        {
            var dlqProperties = new BasicProperties { Persistent = true, ContentType = "application/json" };

            await _channelLock.WaitAsync(stoppingToken);
            try
            {
                await channel.BasicPublishAsync(string.Empty, deadLetterQueue, mandatory: false, dlqProperties, ea.Body, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            finally
            {
                _channelLock.Release();
            }
        }

        TEvent? @event;
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            @event = JsonSerializer.Deserialize<TEvent>(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Poruka iz reda {Queue} se ne može deserijalizovati - ide u dead-letter.", queueName);
            await DeadLetterAndAckAsync();
            return;
        }

        if (@event is null)
        {
            logger.LogError("Prazna poruka iz reda {Queue} - ide u dead-letter.", queueName);
            await DeadLetterAndAckAsync();
            return;
        }

        using (var checkScope = scopeFactory.CreateScope())
        {
            var checkDb = checkScope.ServiceProvider.GetRequiredService<WorkerDbContext>();
            if (await checkDb.ProcessedEvents.AnyAsync(x => x.EventId == @event.EventId, stoppingToken))
            {
                logger.LogInformation("Događaj {EventId} ({EventType}) je već obrađen, preskače se.", @event.EventId, typeof(TEvent).Name);
                await AckAsync();
                return;
            }
        }

        for (var attempt = 0; attempt <= BackoffSeconds.Length; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

                await HandleAsync(@event, scope.ServiceProvider, stoppingToken);

                dbContext.ProcessedEvents.Add(new ProcessedEvent { EventId = @event.EventId, ProcessedAt = DateTime.UtcNow });
                await dbContext.SaveChangesAsync(stoppingToken);

                await AckAsync();
                return;
            }
            catch (Exception ex)
            {
                // Dvije konkurentne isporuke istog EventId-a (rijetko, ali moguce uz prefetch > 1) - ova
                // je izgubila utrku za upis u ProcessedEvents (unique kljuc); tretiraj kao vec obradjeno
                // umjesto da retry-uje/dead-letter-uje isti posao koji je druga isporuka vec zavrsila.
                if (ex is DbUpdateException && await IsAlreadyProcessedAsync(@event.EventId, stoppingToken))
                {
                    logger.LogInformation(
                        "Događaj {EventId} ({EventType}) je konkurentno obrađen u drugom pokušaju, preskače se.",
                        @event.EventId, typeof(TEvent).Name);
                    await AckAsync();
                    return;
                }

                if (attempt < BackoffSeconds.Length)
                {
                    var delaySeconds = BackoffSeconds[attempt];
                    logger.LogWarning(ex,
                        "Obrada događaja {EventId} ({EventType}) neuspjela, pokušaj {Attempt}/{MaxAttempts}, novi pokušaj za {Delay}s.",
                        @event.EventId, typeof(TEvent).Name, attempt + 1, BackoffSeconds.Length, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                }
                else
                {
                    logger.LogError(ex,
                        "Obrada događaja {EventId} ({EventType}) neuspjela nakon {MaxAttempts} pokušaja - ide u dead-letter.",
                        @event.EventId, typeof(TEvent).Name, BackoffSeconds.Length + 1);
                    await DeadLetterAndAckAsync();
                    return;
                }
            }
        }
    }

    private async Task<bool> IsAlreadyProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();
        return await dbContext.ProcessedEvents.AnyAsync(x => x.EventId == eventId, cancellationToken);
    }
}
