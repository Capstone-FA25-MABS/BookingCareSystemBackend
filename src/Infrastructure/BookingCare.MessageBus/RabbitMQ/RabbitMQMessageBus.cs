using BookingCare.MessageBus.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace BookingCare.MessageBus.RabbitMQ;

public class RabbitMQMessageBus : IMessageBus
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQMessageBus> _logger;
    private bool _disposed = false;

    public RabbitMQMessageBus(IConnection connection, ILogger<RabbitMQMessageBus> logger)
    {
        _connection = connection;
        _channel = _connection.CreateModel();
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, string exchangeName, string routingKey = "") where T : class
    {
        try
        {
            _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);

            var jsonMessage = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(jsonMessage);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(exchange: exchangeName,
                                routingKey: routingKey,
                                basicProperties: properties,
                                body: body);

            _logger.LogInformation("Published message to exchange {ExchangeName} with routing key {RoutingKey}", 
                exchangeName, routingKey);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to exchange {ExchangeName}", exchangeName);
            throw;
        }
    }

    public async Task SubscribeAsync<T>(string queueName, string exchangeName, string routingKey, Func<T, Task<bool>> messageHandler) where T : class
    {
        try
        {
            _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: routingKey);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var deserializedMessage = JsonConvert.DeserializeObject<T>(message);

                    if (deserializedMessage != null)
                    {
                        var success = await messageHandler(deserializedMessage);
                        if (success)
                        {
                            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                            _logger.LogInformation("Successfully processed message from queue {QueueName}", queueName);
                        }
                        else
                        {
                            _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                            _logger.LogWarning("Failed to process message from queue {QueueName}, requeued", queueName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("Started consuming messages from queue {QueueName}", queueName);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to queue {QueueName}", queueName);
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
