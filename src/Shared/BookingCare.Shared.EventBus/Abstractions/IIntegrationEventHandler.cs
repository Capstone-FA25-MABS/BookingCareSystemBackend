using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Shared.EventBus.Abstractions;

public interface IIntegrationEventHandler<in TIntegrationEvent> 
    where TIntegrationEvent : IntegrationEvent
{
    Task HandleAsync(TIntegrationEvent @event, CancellationToken cancellationToken = default);
}

public interface IIntegrationEventHandler
{
    Task HandleAsync(IntegrationEvent @event, CancellationToken cancellationToken = default);
}
