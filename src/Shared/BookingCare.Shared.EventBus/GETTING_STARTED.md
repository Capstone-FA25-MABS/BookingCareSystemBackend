# Getting Started with BookingCare Event Bus

## Quick Setup

### 1. Add Project Reference

In your service project, add a reference to the shared event bus:

```xml
<ProjectReference Include="..\..\Shared\BookingCare.Shared.EventBus\BookingCare.Shared.EventBus.csproj" />
```

### 2. Configure Services

In your `Program.cs`:

```csharp
using BookingCare.Shared.EventBus.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Event Bus
builder.Services.AddRabbitMQEventBus(builder.Configuration, "your-service-queue");

// Register your event handlers
builder.Services.AddIntegrationEventHandler<YourEventHandler>();

var app = builder.Build();

// Configure event subscriptions
app.UseEventBus(eventBus =>
{
    eventBus.Subscribe<YourIntegrationEvent, YourEventHandler>();
});

app.Run();
```

### 3. Configuration

Add to your `appsettings.json`:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "ExchangeName": "booking_care_event_bus",
    "QueueName": "your-service-queue"
  }
}
```

### 4. Publishing Events

```csharp
public class UserController : ControllerBase
{
    private readonly IEventBus _eventBus;

    public UserController(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        // Your business logic
        var user = await _userService.CreateAsync(request);

        // Publish event
        await _eventBus.PublishAsync(new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            RegisteredAt = DateTime.UtcNow
        });

        return Ok(user);
    }
}
```

### 5. Handling Events

```csharp
public class UserRegisteredEventHandler : IIntegrationEventHandler<UserRegisteredEvent>
{
    private readonly INotificationService _notificationService;

    public UserRegisteredEventHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task HandleAsync(UserRegisteredEvent @event, CancellationToken cancellationToken = default)
    {
        // Handle the event
        await _notificationService.SendWelcomeEmailAsync(@event.Email, @event.FullName);
    }
}
```

## Available Events

- `UserRegisteredEvent`
- `AppointmentCreatedEvent`
- `PaymentProcessedEvent`
- `NotificationSendEvent`
- And many more...

See `Events/CommonEvents.cs` for all available events.

## Docker Setup

Run RabbitMQ with Docker:

```bash
docker run -d --name rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=admin \
  -e RABBITMQ_DEFAULT_PASS=admin123 \
  rabbitmq:3-management
```

Access management UI at http://localhost:15672 (admin/admin123)

## Health Checks

Health checks are automatically registered. Add to your pipeline:

```csharp
app.MapHealthChecks("/health");
```

## Need Help?

Check the comprehensive README.md for detailed documentation, examples, and troubleshooting.
