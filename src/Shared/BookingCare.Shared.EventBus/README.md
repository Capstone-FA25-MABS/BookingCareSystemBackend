# BookingCare Shared Event Bus

A comprehensive RabbitMQ-based event bus implementation for the BookingCare microservices architecture. This library provides a robust, scalable event-driven communication system for distributed services.

## Features

- **RabbitMQ Integration**: Full RabbitMQ support with automatic reconnection and error handling
- **Typed Events**: Strongly-typed integration events with JSON serialization
- **Retry Policies**: Built-in retry mechanisms using Polly
- **Health Checks**: RabbitMQ connection health monitoring
- **Dependency Injection**: Full .NET dependency injection support
- **Logging**: Comprehensive logging integration
- **Persistent Connections**: Automatic connection recovery and management

## Quick Start

### 1. Installation

Add reference to the shared event bus project:

```xml
<ProjectReference Include="..\..\Shared\BookingCare.Shared.EventBus\BookingCare.Shared.EventBus.csproj" />
```

### 2. Configuration

Add RabbitMQ configuration to your `appsettings.json`:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "booking_care_event_bus",
    "QueueName": "booking_care_queue",
    "RetryCount": 3,
    "ConnectionTimeout": 30000,
    "RequestedHeartbeat": 60,
    "AutomaticRecoveryEnabled": true,
    "NetworkRecoveryInterval": 5000,
    "PersistentMessages": true,
    "AutoDeleteQueue": false,
    "DurableQueue": true,
    "ExclusiveQueue": false
  }
}
```

### 3. Service Registration

In your `Program.cs` or `Startup.cs`:

```csharp
using BookingCare.Shared.EventBus.Extensions;
using BookingCare.Shared.EventBus.Handlers;

// Register event bus
services.AddRabbitMQEventBus(configuration, queueName: "user-service-queue");

// Register event handlers
services.AddIntegrationEventHandler<UserRegisteredEventHandler>();
services.AddIntegrationEventHandler<AppointmentCreatedEventHandler>();

// Or register multiple handlers at once
services.AddIntegrationEventHandlers(
    typeof(UserRegisteredEventHandler),
    typeof(AppointmentCreatedEventHandler),
    typeof(PaymentProcessedEventHandler)
);
```

### 4. Publishing Events

```csharp
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

public class UserController : ControllerBase
{
    private readonly IEventBus _eventBus;

    public UserController(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterUser(RegisterUserRequest request)
    {
        // Your user registration logic here
        var user = await _userService.RegisterAsync(request);

        // Publish integration event
        var @event = new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            RegisteredAt = DateTime.UtcNow
        };

        await _eventBus.PublishAsync(@event);

        return Ok(user);
    }
}
```

### 5. Subscribing to Events

```csharp
using BookingCare.Shared.EventBus.Abstractions;

public class UserRegisteredEventHandler : IIntegrationEventHandler<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredEventHandler> _logger;
    private readonly INotificationService _notificationService;

    public UserRegisteredEventHandler(
        ILogger<UserRegisteredEventHandler> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task HandleAsync(UserRegisteredEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing user registration for {UserId}", @event.UserId);

        // Send welcome email
        await _notificationService.SendWelcomeEmailAsync(@event.Email, @event.FullName);

        // Create user profile
        await _profileService.CreateDefaultProfileAsync(@event.UserId);

        _logger.LogInformation("User registration processed successfully for {UserId}", @event.UserId);
    }
}
```

### 6. Service Startup

In your service startup, configure subscriptions:

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        // Configure event subscriptions
        ConfigureEventBus(host.Services);

        await host.RunAsync();
    }

    private static void ConfigureEventBus(IServiceProvider services)
    {
        var eventBus = services.GetRequiredService<IEventBus>();

        // Subscribe to events
        eventBus.Subscribe<UserRegisteredEvent, UserRegisteredEventHandler>();
        eventBus.Subscribe<AppointmentCreatedEvent, AppointmentCreatedEventHandler>();
        eventBus.Subscribe<PaymentProcessedEvent, PaymentProcessedEventHandler>();

        // Subscribe with custom routing key
        eventBus.Subscribe<NotificationSendEvent, NotificationSendEventHandler>("notification.email");

        // Start consuming
        eventBus.StartConsuming();
    }
}
```

## Available Events

The library includes predefined integration events for common BookingCare scenarios:

### User Events
- `UserRegisteredEvent`
- `UserProfileUpdatedEvent`
- `UserDeletedEvent`

### Doctor Events
- `DoctorRegisteredEvent`
- `DoctorProfileUpdatedEvent`

### Appointment Events
- `AppointmentCreatedEvent`
- `AppointmentUpdatedEvent`
- `AppointmentCancelledEvent`
- `AppointmentCompletedEvent`

### Payment Events
- `PaymentProcessedEvent`
- `PaymentFailedEvent`
- `RefundProcessedEvent`

### Notification Events
- `NotificationSendEvent`

### Review Events
- `ReviewCreatedEvent`

### Analytics Events
- `UserActivityEvent`

## Creating Custom Events

To create a custom integration event:

```csharp
using BookingCare.Shared.EventBus.Events;

public class CustomEvent : IntegrationEvent
{
    public string CustomProperty { get; set; } = string.Empty;
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
}

// Handler
public class CustomEventHandler : IIntegrationEventHandler<CustomEvent>
{
    public async Task HandleAsync(CustomEvent @event, CancellationToken cancellationToken = default)
    {
        // Handle your custom event
        await Task.CompletedTask;
    }
}
```

## Health Checks

The event bus includes health checks for RabbitMQ connectivity:

```csharp
// In Program.cs
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

## Docker Compose Configuration

Add RabbitMQ to your `docker-compose.yml`:

```yaml
services:
  rabbitmq:
    image: rabbitmq:3-management
    container_name: booking_care_rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: admin123
    ports:
      - "5672:5672"
      - "15672:15672"
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    networks:
      - booking-care-network

volumes:
  rabbitmq_data:

networks:
  booking-care-network:
    driver: bridge
```

## Configuration Options

| Property | Default | Description |
|----------|---------|-------------|
| `HostName` | localhost | RabbitMQ server hostname |
| `Port` | 5672 | RabbitMQ server port |
| `UserName` | guest | Authentication username |
| `Password` | guest | Authentication password |
| `VirtualHost` | / | RabbitMQ virtual host |
| `ExchangeName` | booking_care_event_bus | Exchange name for events |
| `QueueName` | booking_care_queue | Default queue name |
| `RetryCount` | 3 | Number of retry attempts |
| `ConnectionTimeout` | 30000 | Connection timeout in ms |
| `RequestedHeartbeat` | 60 | Heartbeat interval in seconds |
| `AutomaticRecoveryEnabled` | true | Enable automatic recovery |
| `NetworkRecoveryInterval` | 5000 | Recovery interval in ms |
| `PersistentMessages` | true | Message persistence |
| `DurableQueue` | true | Queue durability |

## Error Handling

The event bus includes comprehensive error handling:

- **Connection Issues**: Automatic reconnection with exponential backoff
- **Message Processing Errors**: Logging and acknowledgment to prevent message loss
- **Serialization Errors**: Detailed logging with message content
- **Handler Exceptions**: Isolated handling to prevent cascade failures

## Best Practices

1. **Event Design**: Keep events immutable and focused on business facts
2. **Handler Idempotency**: Ensure handlers can process the same event multiple times safely
3. **Error Handling**: Implement proper error handling and compensation logic
4. **Monitoring**: Use health checks and logging for production monitoring
5. **Testing**: Test event handlers in isolation and integration scenarios

## Monitoring and Troubleshooting

### Logging

The event bus provides detailed logging at various levels:

```csharp
// Enable detailed logging in appsettings.json
{
  "Logging": {
    "LogLevel": {
      "BookingCare.Shared.EventBus": "Debug",
      "RabbitMQ.Client": "Warning"
    }
  }
}
```

### RabbitMQ Management

Access the RabbitMQ management interface at `http://localhost:15672` (default: guest/guest)

### Common Issues

1. **Connection Refused**: Check RabbitMQ is running and accessible
2. **Authentication Failed**: Verify username/password in configuration
3. **Queue Not Found**: Ensure queue is declared before consuming
4. **Messages Not Processing**: Check handler registration and logging

For more advanced scenarios and troubleshooting, refer to the RabbitMQ documentation.
