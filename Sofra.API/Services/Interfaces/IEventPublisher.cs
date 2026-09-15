using Sofra.Shared.Events;

namespace Sofra.API.Services.Interfaces;

public interface IEventPublisher
{
    /// <summary>
    /// Objavljuje dogadjaj na sofra.events exchange sa datim routing key-em. Nikad ne baca izuzetak -
    /// ako RabbitMQ nije dostupan, greska se loguje i zahtjev korisnika nastavlja normalno.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, string routingKey, CancellationToken cancellationToken = default) where TEvent : class, IEvent;
}
