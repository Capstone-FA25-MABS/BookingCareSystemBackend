using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace BookingCare.EventPublisher;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        var publisher = host.Services.GetRequiredService<TestEventPublisher>();
        
        Console.WriteLine("BookingCare Event Publisher");
        Console.WriteLine("==========================");
        Console.WriteLine("1. User Registered Event");
        Console.WriteLine("2. Appointment Created Event");
        Console.WriteLine("3. Payment Processed Event");
        Console.WriteLine("4. Custom Event");
        Console.WriteLine("q. Quit");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Select event to publish (1-4, q): ");
            var input = Console.ReadLine();

            try
            {
                switch (input?.ToLower())
                {
                    case "1":
                        await publisher.PublishUserRegisteredEvent();
                        break;
                    case "2":
                        await publisher.PublishAppointmentCreatedEvent();
                        break;
                    case "3":
                        await publisher.PublishPaymentProcessedEvent();
                        break;
                    case "4":
                        await publisher.PublishCustomEvent();
                        break;
                    case "q":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        continue;
                }

                Console.WriteLine("✅ Event published successfully!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error publishing event: {ex.Message}\n");
            }
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.Configure<RabbitMQConfig>(
                    context.Configuration.GetSection("RabbitMQ"));
                
                services.AddSingleton<TestEventPublisher>();
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });
}

public class RabbitMQConfig
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "bookingcare";
    public string Password { get; set; } = "bookingcare@1234";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "booking_care_event_bus";
}

public class TestEventPublisher
{
    private readonly RabbitMQConfig _config;
    private readonly ILogger<TestEventPublisher> _logger;

    public TestEventPublisher(Microsoft.Extensions.Options.IOptions<RabbitMQConfig> config, ILogger<TestEventPublisher> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task PublishUserRegisteredEvent()
    {
        var @event = new
        {
            Id = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            EventName = "UserRegisteredEvent",
            UserId = Guid.NewGuid(),
            Email = "john.doe@example.com",
            FullName = "John Doe",
            Role = "Patient",
            RegisteredAt = DateTime.UtcNow
        };

        await PublishEvent(@event, "UserRegisteredEvent");
    }

    public async Task PublishAppointmentCreatedEvent()
    {
        var @event = new
        {
            Id = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            EventName = "AppointmentCreatedEvent",
            AppointmentId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            ClinicId = Guid.NewGuid(),
            AppointmentDate = DateTime.UtcNow.AddDays(7),
            Status = "Scheduled",
            Amount = 150.00m,
            CreatedAt = DateTime.UtcNow
        };

        await PublishEvent(@event, "AppointmentCreatedEvent");
    }

    public async Task PublishPaymentProcessedEvent()
    {
        var @event = new
        {
            Id = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            EventName = "PaymentProcessedEvent",
            PaymentId = Guid.NewGuid(),
            AppointmentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 150.00m,
            PaymentMethod = "Credit Card",
            Status = "Completed",
            TransactionId = "TXN_" + DateTime.UtcNow.Ticks,
            ProcessedAt = DateTime.UtcNow
        };

        await PublishEvent(@event, "PaymentProcessedEvent");
    }

    public async Task PublishCustomEvent()
    {
        Console.Write("Enter event name: ");
        var eventName = Console.ReadLine() ?? "CustomEvent";
        
        Console.Write("Enter custom message: ");
        var customMessage = Console.ReadLine() ?? "Test message";

        var @event = new
        {
            Id = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            EventName = eventName,
            Message = customMessage,
            Timestamp = DateTime.UtcNow,
            Source = "TestPublisher"
        };

        await PublishEvent(@event, eventName);
    }

    private async Task PublishEvent(object @event, string routingKey)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config.HostName,
            Port = _config.Port,
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost
        };

        using var connection = factory.CreateConnection("BookingCare-TestPublisher");
        using var channel = connection.CreateModel();

        // Declare exchange
        channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Topic, durable: true);

        var message = JsonSerializer.Serialize(@event, new JsonSerializerOptions { WriteIndented = true });
        var body = Encoding.UTF8.GetBytes(message);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.Type = routingKey;

        channel.BasicPublish(
            exchange: _config.ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published event: {EventName} with routing key: {RoutingKey}", @event.GetType().Name, routingKey);
        
        await Task.CompletedTask;
    }
}
