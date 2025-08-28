# 🐰 RabbitMQ Setup & Event Monitoring Guide

## Quick Start

### 1. Start RabbitMQ Container

```bash
cd /Users/hieumaixuan/Documents/capstone-src/BookingCareSystemBackend
./scripts/setup-rabbitmq.sh
```

**Or manually:**
```bash
docker-compose -f docker-compose.rabbitmq.yml up -d
```

### 2. Access RabbitMQ Management UI

- **URL**: http://localhost:15672
- **Username**: `bookingcare`
- **Password**: `bookingcare@1234`

## 🔧 Connection Details

| Service | Host | Port | Credentials |
|---------|------|------|-------------|
| AMQP | localhost | 5672 | bookingcare / bookingcare@1234 |
| Management UI | localhost | 15672 | bookingcare / bookingcare@1234 |

## 📊 Event Monitoring Tools

### 1. Event Monitor (Real-time Console)

Monitors all events in real-time with colored output:

```bash
cd tools/EventMonitor
dotnet run
```

**Features:**
- 🔔 Real-time event notifications
- 🎨 Color-coded output
- 📄 JSON formatting
- 🗝️ Routing key display
- 📋 Header information

### 2. Event Publisher (Test Tool)

Publishes test events for demonstration:

```bash
cd tools/EventPublisher
dotnet run
```

**Available Events:**
1. User Registered Event
2. Appointment Created Event
3. Payment Processed Event
4. Custom Event

### 3. RabbitMQ Management UI

Web interface for comprehensive monitoring:

**Access**: http://localhost:15672

**Key Features:**
- 📈 Real-time metrics and graphs
- 📋 Queue and exchange management
- 🔍 Message inspection
- 👥 Connection monitoring
- ⚙️ Configuration management

## 🎯 Testing the Event System

### Step 1: Start RabbitMQ
```bash
./scripts/setup-rabbitmq.sh
```

### Step 2: Start Event Monitor
```bash
cd tools/EventMonitor
dotnet run
```

### Step 3: Publish Test Events
In a new terminal:
```bash
cd tools/EventPublisher
dotnet run
```

### Step 4: Monitor in Management UI
1. Open http://localhost:15672
2. Login with `bookingcare` / `bookingcare@1234`
3. Go to "Queues" tab
4. Click on queue names to see messages

## 📈 Management UI Navigation

### Overview Tab
- System resource usage
- Message rates
- Connection status

### Connections Tab
- Active connections
- Client information
- Channel details

### Channels Tab
- Active channels
- Message rates per channel
- Consumer information

### Exchanges Tab
- Exchange list and types
- Routing configuration
- Message rates

### Queues Tab
- Queue statistics
- Message counts
- Consumer information
- **Get Messages**: Inspect message content

### Admin Tab
- User management
- Virtual host configuration
- Policies and parameters

## 🔍 Key Monitoring Points

### Exchange Information
- **Name**: `booking_care_event_bus`
- **Type**: `topic`
- **Durability**: `durable`

### Routing Keys to Watch
- `UserRegisteredEvent`
- `AppointmentCreatedEvent`
- `PaymentProcessedEvent`
- `NotificationSendEvent`
- Custom event names

### Queue Patterns
- Service-specific queues: `user-service-queue`, `appointment-service-queue`
- Temporary monitor queues: `event-monitor-*`

## 🛠️ Troubleshooting

### Connection Issues
```bash
# Check if container is running
docker ps | grep rabbitmq

# Check container logs
docker logs bookingcare_rabbitmq

# Restart container
docker-compose -f docker-compose.rabbitmq.yml restart
```

### Port Conflicts
```bash
# Check what's using the ports
lsof -i :5672
lsof -i :15672

# Stop conflicting services or change ports in docker-compose.rabbitmq.yml
```

### Authentication Issues
- Verify credentials in configuration files
- Check user permissions in Management UI
- Reset container if needed:
  ```bash
  docker-compose -f docker-compose.rabbitmq.yml down -v
  docker-compose -f docker-compose.rabbitmq.yml up -d
  ```

## 🔄 Integration with BookingCare Services

### Configuration for Services
Use the configuration from `appsettings.example.json`:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "bookingcare",
    "Password": "bookingcare@1234",
    "VirtualHost": "/",
    "ExchangeName": "booking_care_event_bus",
    "QueueName": "your-service-queue"
  }
}
```

### Service Registration
```csharp
// In Program.cs
builder.Services.AddRabbitMQEventBus(builder.Configuration, "service-specific-queue");

// Configure event subscriptions
app.UseEventBus(eventBus =>
{
    eventBus.Subscribe<UserRegisteredEvent, UserRegisteredEventHandler>();
    // Add more subscriptions...
});
```

## 📝 Event Examples

### Publishing an Event
```csharp
await _eventBus.PublishAsync(new UserRegisteredEvent
{
    UserId = Guid.NewGuid(),
    Email = "user@example.com",
    FullName = "John Doe",
    Role = "Patient",
    RegisteredAt = DateTime.UtcNow
});
```

### Handling an Event
```csharp
public class UserRegisteredEventHandler : IIntegrationEventHandler<UserRegisteredEvent>
{
    public async Task HandleAsync(UserRegisteredEvent @event, CancellationToken cancellationToken)
    {
        // Process the event
        Console.WriteLine($"User registered: {@event.Email}");
    }
}
```

## 🚀 Production Considerations

### Security
- Change default credentials
- Use environment variables for secrets
- Enable SSL/TLS for production

### Monitoring
- Set up alerts for queue lengths
- Monitor message rates
- Track connection health

### Scaling
- Use clustering for high availability
- Configure queue mirroring
- Set up load balancing

---

## 🎉 You're Ready!

Your RabbitMQ event system is now set up with:
- ✅ Docker container running
- ✅ Management UI accessible
- ✅ Event monitoring tools ready
- ✅ Test publisher available
- ✅ Integration documentation complete

Start monitoring events and testing your event-driven architecture!
