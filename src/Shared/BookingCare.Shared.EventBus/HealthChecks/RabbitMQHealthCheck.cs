using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using BookingCare.Shared.EventBus.Configuration;
using BookingCare.Shared.EventBus.Connection;

namespace BookingCare.Shared.EventBus.HealthChecks;

public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly IRabbitMQPersistentConnection _connection;
    private readonly ILogger<RabbitMQHealthCheck> _logger;
    private readonly RabbitMQConfiguration _config;

    public RabbitMQHealthCheck(
        IRabbitMQPersistentConnection connection,
        ILogger<RabbitMQHealthCheck> logger,
        IOptions<RabbitMQConfiguration> config)
    {
        _connection = connection;
        _logger = logger;
        _config = config.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connection.IsConnected)
            {
                using var channel = _connection.CreateModel();

                // Test basic operations
                var queueName = $"health-check-{Guid.NewGuid()}";
                channel.QueueDeclare(queue: queueName, durable: false, exclusive: true, autoDelete: true);
                channel.QueueDelete(queueName);

                _logger.LogDebug("RabbitMQ health check passed");

                return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ connection is healthy"));
            }
            else
            {
                // Try to reconnect
                if (_connection.TryConnect())
                {
                    return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ connection restored"));
                }
                else
                {
                    _logger.LogWarning("RabbitMQ health check failed - unable to connect");
                    return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ connection is not available"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ health check failed with exception");
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ health check failed", ex));
        }
    }
}
