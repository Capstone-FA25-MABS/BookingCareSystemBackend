using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using BookingCare.Shared.EventBus.Configuration;

namespace BookingCare.Shared.EventBus.Connection;

public interface IRabbitMQPersistentConnection : IDisposable
{
    bool IsConnected { get; }
    bool TryConnect();
    IModel CreateModel();
    void Disconnect();
}

public class RabbitMQPersistentConnection : IRabbitMQPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQPersistentConnection> _logger;
    private readonly RabbitMQConfiguration _config;
    private IConnection? _connection;
    private bool _disposed;

    private readonly object _syncRoot = new object();

    public RabbitMQPersistentConnection(
        IConnectionFactory connectionFactory,
        ILogger<RabbitMQPersistentConnection> logger,
        IOptions<RabbitMQConfiguration> config)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public IModel CreateModel()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }

        return _connection!.CreateModel();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            _connection?.Dispose();
        }
        catch (IOException ex)
        {
            _logger.LogCritical(ex, "Error disposing RabbitMQ connection");
        }
    }

    public bool TryConnect()
    {
        _logger.LogInformation("RabbitMQ Client is trying to connect");

        lock (_syncRoot)
        {
            try
            {
                _connection = _connectionFactory.CreateConnection();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "FATAL ERROR: RabbitMQ connections could not be created and opened");
                return false;
            }

            if (IsConnected)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;

                _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);

                return true;
            }
            else
            {
                _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");
                return false;
            }
        }
    }

    /// <summary>
    /// Disconnect from the RabbitMQ server
    /// </summary>
    public void Disconnect()
    {
        if (_disposed) return;

        try
        {
            _connection?.Close();
        }
        catch (IOException ex)
        {
            _logger.LogCritical(ex, "Error disconnecting from RabbitMQ");
        }
    }

    private void OnConnectionBlocked(object? sender, RabbitMQ.Client.Events.ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect... Reason: {Reason}", e.Reason);

        TryConnect();
    }

    private void OnCallbackException(object? sender, RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect... Exception: {Exception}", e.Exception);

        TryConnect();
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect... Reason: {ReasonText}", reason.ReplyText);

        TryConnect();
    }
}
