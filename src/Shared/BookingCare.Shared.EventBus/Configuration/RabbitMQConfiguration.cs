namespace BookingCare.Shared.EventBus.Configuration;

public class RabbitMQConfiguration
{
    public const string SectionName = "RabbitMQ";
    
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "booking_care_event_bus";
    public string QueueName { get; set; } = "booking_care_queue";
    public int RetryCount { get; set; } = 3;
    public int ConnectionTimeout { get; set; } = 30000; // 30 seconds
    public int RequestedHeartbeat { get; set; } = 60; // 60 seconds
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public int NetworkRecoveryInterval { get; set; } = 5000; // 5 seconds
    public bool PersistentMessages { get; set; } = true;
    public bool AutoDeleteQueue { get; set; } = false;
    public bool DurableQueue { get; set; } = true;
    public bool ExclusiveQueue { get; set; } = false;
}
