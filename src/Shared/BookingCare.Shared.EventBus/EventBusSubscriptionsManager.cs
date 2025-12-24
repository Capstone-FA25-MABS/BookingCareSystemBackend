using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Shared.EventBus;

public class EventBusSubscriptionsManager : IEventBusSubscriptionsManager
{
    private readonly Dictionary<string, List<Type>> _handlers = new();
    private readonly Dictionary<string, Type> _eventTypes = new();
    private readonly Dictionary<string, string> _routingKeys = new();

    public event EventHandler<string>? OnEventRemoved;

    public bool IsEmpty => !_handlers.Keys.Any();

    public void AddSubscription<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        var eventName = GetEventKey<T>();
        AddSubscription(typeof(TH), eventName);

        if (!_eventTypes.ContainsKey(eventName))
        {
            _eventTypes.Add(eventName, typeof(T));
        }
    }

    public void AddSubscription<T, TH>(string routingKey)
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        var eventName = GetEventKey<T>();
        AddSubscription(typeof(TH), eventName);

        if (!_eventTypes.ContainsKey(eventName))
        {
            _eventTypes.Add(eventName, typeof(T));
        }

        if (!_routingKeys.ContainsKey(eventName))
        {
            _routingKeys.Add(eventName, routingKey);
        }
    }

    private void AddSubscription(Type handlerType, string eventName)
    {
        if (!HasSubscriptionsForEvent(eventName))
        {
            _handlers.Add(eventName, new List<Type>());
        }

        if (_handlers[eventName].Any(s => s == handlerType))
        {
            throw new ArgumentException($"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
        }

        _handlers[eventName].Add(handlerType);
    }

    public void RemoveSubscription<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        var handlerToRemove = FindSubscriptionToRemove<T, TH>();
        var eventName = GetEventKey<T>();
        RemoveHandler(eventName, handlerToRemove);
    }

    private Type? FindSubscriptionToRemove<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        var eventName = GetEventKey<T>();
        return FindSubscriptionToRemove(eventName, typeof(TH));
    }

    private Type? FindSubscriptionToRemove(string eventName, Type handlerType)
    {
        if (!HasSubscriptionsForEvent(eventName))
        {
            return null;
        }

        return _handlers[eventName].SingleOrDefault(s => s == handlerType);
    }

    private void RemoveHandler(string eventName, Type? subsToRemove)
    {
        if (subsToRemove != null)
        {
            _handlers[eventName].Remove(subsToRemove);

            if (!_handlers[eventName].Any())
            {
                _handlers.Remove(eventName);
                var eventType = _eventTypes[eventName];
                if (eventType != null)
                {
                    _eventTypes.Remove(eventName);
                }

                _routingKeys.Remove(eventName);
                RaiseOnEventRemoved(eventName);
            }
        }
    }

    public bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent
    {
        var key = GetEventKey<T>();
        return HasSubscriptionsForEvent(key);
    }

    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    public Type? GetEventTypeByName(string eventName) => _eventTypes.TryGetValue(eventName, out var eventType) ? eventType : null;

    public void Clear() => _handlers.Clear();

    public IEnumerable<Type> GetHandlersForEvent<T>() where T : IntegrationEvent
    {
        var key = GetEventKey<T>();
        return GetHandlersForEvent(key);
    }

    public IEnumerable<Type> GetHandlersForEvent(string eventName) => _handlers.TryGetValue(eventName, out var handlers) ? handlers : Array.Empty<Type>();

    private void RaiseOnEventRemoved(string eventName) => OnEventRemoved?.Invoke(this, eventName);

    public string GetEventKey<T>() where T : IntegrationEvent => GetEventKey(typeof(T));

    public string GetEventKey(Type eventType) => eventType.Name;

    public string? GetRoutingKey(string eventName) => _routingKeys.TryGetValue(eventName, out var routingKey) ? routingKey : null;
}
