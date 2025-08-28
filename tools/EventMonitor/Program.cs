using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BookingCare.EventMonitor;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        var monitor = host.Services.GetRequiredService<EventMonitor>();
        await monitor.StartMonitoringAsync();
        
        Console.WriteLine("Press any key to stop monitoring...");
        Console.ReadKey();
        
        await monitor.StopMonitoringAsync();
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.Configure<RabbitMQConfig>(
                    context.Configuration.GetSection("RabbitMQ"));
                
                services.AddSingleton<EventMonitor>();
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

public class EventMonitor
{
    private readonly RabbitMQConfig _config;
    private readonly ILogger<EventMonitor> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public EventMonitor(Microsoft.Extensions.Options.IOptions<RabbitMQConfig> config, ILogger<EventMonitor> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task StartMonitoringAsync()
    {
        try
        {
            _logger.LogInformation("Starting Event Monitor...");
            
            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password,
                VirtualHost = _config.VirtualHost,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection("BookingCare-EventMonitor");
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Topic, durable: true);

            // Create a temporary queue for monitoring all events
            var queueName = _channel.QueueDeclare(
                queue: "event-monitor-" + Guid.NewGuid().ToString("N")[..8],
                durable: false,
                exclusive: true,
                autoDelete: true).QueueName;

            // Bind to all events using wildcard
            _channel.QueueBind(queueName, _config.ExchangeName, "#");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnEventReceived;

            _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

            _logger.LogInformation("Event Monitor started. Listening for events on exchange: {ExchangeName}", _config.ExchangeName);
            _logger.LogInformation("Queue: {QueueName}", queueName);
            _logger.LogInformation("=====================================");

            await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Event monitoring stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in event monitoring");
        }
    }

    private async Task OnEventReceived(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var routingKey = ea.RoutingKey;
            var exchange = ea.Exchange;
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"🔔 EVENT RECEIVED [{timestamp}]");
            Console.ResetColor();
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"📍 Exchange: {exchange}");
            Console.WriteLine($"🗝️  Routing Key: {routingKey}");
            Console.ResetColor();
            
            // Try to parse as JSON for better formatting
            try
            {
                var jsonDoc = JsonDocument.Parse(message);
                var formattedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("📄 Event Data:");
                Console.ResetColor();
                Console.WriteLine(formattedJson);
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("📄 Raw Message:");
                Console.ResetColor();
                Console.WriteLine(message);
            }
            
            // Show properties if available
            if (ea.BasicProperties?.Headers?.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("📋 Headers:");
                Console.ResetColor();
                foreach (var header in ea.BasicProperties.Headers)
                {
                    var value = Encoding.UTF8.GetString((byte[])header.Value);
                    Console.WriteLine($"  {header.Key}: {value}");
                }
            }
            
            Console.WriteLine("=====================================");
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing received event");
        }
    }

    public async Task StopMonitoringAsync()
    {
        _cancellationTokenSource.Cancel();
        
        _channel?.Close();
        _connection?.Close();
        
        _channel?.Dispose();
        _connection?.Dispose();
        
        _logger.LogInformation("Event Monitor stopped.");
        await Task.CompletedTask;
    }
}
