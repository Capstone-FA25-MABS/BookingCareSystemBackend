using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Shared.EventBus.Abstractions;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, string? routingKey = null, CancellationToken cancellationToken = default)
        where T : IntegrationEvent;

    void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>;

    void Subscribe<T, TH>(string routingKey)
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>;

    void Unsubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>;

    void StartConsuming();
    void StopConsuming();
}
