using System.Text.Json.Serialization;

namespace BookingCare.Shared.EventBus.Events;

public abstract class IntegrationEvent
{
    [JsonConstructor]
    protected IntegrationEvent(Guid id, DateTime createdDate)
    {
        Id = id;
        CreatedDate = createdDate;
    }

    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreatedDate = DateTime.UtcNow;
    }

    [JsonInclude]
    public Guid Id { get; private init; }

    [JsonInclude]
    public DateTime CreatedDate { get; private init; }

    public string EventName => GetType().Name;
}
