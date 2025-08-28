using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Configuration;
using BookingCare.Shared.EventBus.Connection;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Shared.EventBus;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IRabbitMQPersistentConnection _persistentConnection;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQConfiguration _config;

    private IModel? _consumerChannel;
    private string? _queueName;

    public RabbitMQEventBus(
        IRabbitMQPersistentConnection persistentConnection,
        ILogger<RabbitMQEventBus> logger,
        IEventBusSubscriptionsManager subsManager,
        IServiceProvider serviceProvider,
        IOptions<RabbitMQConfiguration> config,
        string? queueName = null)
    {
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subsManager = subsManager ?? throw new ArgumentNullException(nameof(subsManager));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _queueName = queueName ?? _config.QueueName;
        _consumerChannel = CreateConsumerChannel();
        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    private void SubsManager_OnEventRemoved(object? sender, string eventName)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        using var channel = _persistentConnection.CreateModel();
        channel.QueueUnbind(queue: _queueName,
                          exchange: _config.ExchangeName,
                          routingKey: eventName);

        if (_subsManager.IsEmpty)
        {
            _queueName = string.Empty;
            _consumerChannel?.Close();
        }
    }

    public async Task PublishAsync<T>(T @event, string? routingKey = null, CancellationToken cancellationToken = default) 
        where T : IntegrationEvent
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var eventName = @event.GetType().Name;
        var finalRoutingKey = routingKey ?? eventName;

        _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, eventName);

        using var channel = _persistentConnection.CreateModel();

        _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

        channel.ExchangeDeclare(exchange: _config.ExchangeName, type: "topic", durable: true);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions
        {
            WriteIndented = true
        });

        ExecuteWithRetry(() =>
        {
            var properties = channel.CreateBasicProperties();
            properties.DeliveryMode = _config.PersistentMessages ? (byte)2 : (byte)1; // persistent
            properties.MessageId = @event.Id.ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = eventName;

            _logger.LogTrace("Publishing event to RabbitMQ: {EventId} with routing key: {RoutingKey}", @event.Id, finalRoutingKey);

            channel.BasicPublish(
                exchange: _config.ExchangeName,
                routingKey: finalRoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);
        });

        await Task.CompletedTask;

        _logger.LogTrace("Published event to RabbitMQ: {EventId}", @event.Id);
    }

    public void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();
        DoInternalSubscription(eventName);

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).GetGenericTypeName());

        _subsManager.AddSubscription<T, TH>();
        StartBasicConsume();
    }

    public void Subscribe<T, TH>(string routingKey)
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();
        DoInternalSubscription(eventName, routingKey);

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler} and routing key {RoutingKey}", 
            eventName, typeof(TH).GetGenericTypeName(), routingKey);

        _subsManager.AddSubscription<T, TH>(routingKey);
        StartBasicConsume();
    }

    private void DoInternalSubscription(string eventName, string? routingKey = null)
    {
        var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
        if (!containsKey)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            _consumerChannel?.QueueBind(queue: _queueName,
                                      exchange: _config.ExchangeName,
                                      routingKey: routingKey ?? eventName);
        }
    }

    public void Unsubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();

        _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

        _subsManager.RemoveSubscription<T, TH>();
    }

    public void StartConsuming()
    {
        StartBasicConsume();
    }

    public void StopConsuming()
    {
        _consumerChannel?.Close();
    }

    private void StartBasicConsume()
    {
        _logger.LogTrace("Starting RabbitMQ basic consume");

        if (_consumerChannel != null)
        {
            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

            consumer.Received += Consumer_Received;

            _consumerChannel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: consumer);
        }
        else
        {
            _logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
        }
    }

    private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            if (message.ToLowerInvariant().Contains("throw-fake-exception"))
            {
                throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
            }

            await ProcessEvent(eventName, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
        }

        // Even on exception we take the message off the queue.
        // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
        // For more information see: https://www.rabbitmq.com/dlx.html
        _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
    }

    private IModel CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        _logger.LogTrace("Creating RabbitMQ consumer channel");

        var channel = _persistentConnection.CreateModel();

        channel.ExchangeDeclare(exchange: _config.ExchangeName, type: "topic", durable: true);

        channel.QueueDeclare(queue: _queueName,
                             durable: _config.DurableQueue,
                             exclusive: _config.ExclusiveQueue,
                             autoDelete: _config.AutoDeleteQueue,
                             arguments: null);

        channel.CallbackException += (sender, ea) =>
        {
            _logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");

            _consumerChannel?.Dispose();
            _consumerChannel = CreateConsumerChannel();
            StartBasicConsume();
        };

        return channel;
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);

        if (_subsManager.HasSubscriptionsForEvent(eventName))
        {
            using var scope = _serviceProvider.CreateScope();
            var subscriptions = _subsManager.GetHandlersForEvent(eventName);
            foreach (var subscription in subscriptions)
            {
                var handler = scope.ServiceProvider.GetService(subscription);
                if (handler == null) continue;

                var eventType = _subsManager.GetEventTypeByName(eventName);
                if (eventType == null) continue;

                var integrationEvent = JsonSerializer.Deserialize(message, eventType, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });

                var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

                await Task.Yield();
                await (Task)concreteType.GetMethod("HandleAsync")!.Invoke(handler, new[] { integrationEvent, CancellationToken.None })!;
            }
        }
        else
        {
            _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
        }
    }

    private void ExecuteWithRetry(Action action)
    {
        var retryCount = 0;
        var maxRetries = _config.RetryCount;

        while (retryCount <= maxRetries)
        {
            try
            {
                action();
                return;
            }
            catch (BrokerUnreachableException ex) when (retryCount < maxRetries)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                _logger.LogWarning(ex, "Could not publish event, retry {RetryCount}/{MaxRetries} after {Delay}s", 
                    retryCount, maxRetries, delay.TotalSeconds);
                
                Thread.Sleep(delay);
            }
            catch (SocketException ex) when (retryCount < maxRetries)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                _logger.LogWarning(ex, "Could not publish event, retry {RetryCount}/{MaxRetries} after {Delay}s", 
                    retryCount, maxRetries, delay.TotalSeconds);
                
                Thread.Sleep(delay);
            }
        }
    }

    public void Dispose()
    {
        _consumerChannel?.Dispose();
        _subsManager.Clear();
    }
}

internal static class GenericTypeExtensions
{
    public static string GetGenericTypeName(this Type type)
    {
        var typeName = string.Empty;

        if (type.IsGenericType)
        {
            var genericTypes = string.Join(",", type.GetGenericArguments().Select(t => t.Name).ToArray());
            typeName = $"{type.Name.Remove(type.Name.IndexOf('`'))}<{genericTypes}>";
        }
        else
        {
            typeName = type.Name;
        }

        return typeName;
    }

    public static string GetGenericTypeName(this object @object)
    {
        return @object.GetType().GetGenericTypeName();
    }
}
