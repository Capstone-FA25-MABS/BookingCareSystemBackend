namespace BookingCare.Shared.EventBus.Abstractions;

public interface IEventBusSubscriptionsManager
{
    bool IsEmpty { get; }

    event EventHandler<string> OnEventRemoved;

    void AddSubscription<T, TH>()
        where T : Events.IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>;

    void AddSubscription<T, TH>(string routingKey)
        where T : Events.IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>;

    void RemoveSubscription<T, TH>()
        where T : Events.IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>;

    bool HasSubscriptionsForEvent<T>() where T : Events.IntegrationEvent;
    bool HasSubscriptionsForEvent(string eventName);

    Type? GetEventTypeByName(string eventName);
    void Clear();

    IEnumerable<Type> GetHandlersForEvent<T>() where T : Events.IntegrationEvent;
    IEnumerable<Type> GetHandlersForEvent(string eventName);

    string GetEventKey<T>() where T : Events.IntegrationEvent;
    string GetEventKey(Type eventType);
}
