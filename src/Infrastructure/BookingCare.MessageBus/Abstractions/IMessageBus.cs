namespace BookingCare.MessageBus.Abstractions;

public interface IMessageBus
{
    Task PublishAsync<T>(T message, string exchangeName, string routingKey = "") where T : class;
    Task SubscribeAsync<T>(string queueName, string exchangeName, string routingKey, Func<T, Task<bool>> messageHandler) where T : class;
    void Dispose();
}

public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime CreatedAt { get; }
}

public abstract class IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}

public interface IEventHandler<in T> where T : IIntegrationEvent
{
    Task HandleAsync(T @event);
}
