using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingCare.Shared.Saga.Manager;

/// <summary>
/// Main saga manager that orchestrates saga execution and event handling
/// </summary>
public class SagaManager : ISagaManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISagaOrchestrator _orchestrator;
    private readonly ISagaStateStore _stateStore;
    private readonly ILogger<SagaManager> _logger;

    public SagaManager(
        IServiceProvider serviceProvider,
        ISagaOrchestrator orchestrator,
        ISagaStateStore stateStore,
        ILogger<SagaManager> logger)
    {
        _serviceProvider = serviceProvider;
        _orchestrator = orchestrator;
        _stateStore = stateStore;
        _logger = logger;
    }

    public Task<Guid> StartSagaAsync<TSaga>(SagaContext context, CancellationToken cancellationToken = default)
        where TSaga : class, ISagaDefinition
    {
        try
        {
            var sagaId = Guid.NewGuid();
            context.SagaId = sagaId;

            var sagaDefinition = _serviceProvider.GetRequiredService<TSaga>();
            context.SagaName = sagaDefinition.SagaName;

            _logger.LogInformation("Starting saga: {SagaName} with SagaId: {SagaId}",
                sagaDefinition.SagaName, sagaId);

            // Execute saga asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await _orchestrator.ExecuteAsync(sagaId, context, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing saga {SagaName} with SagaId: {SagaId}",
                        sagaDefinition.SagaName, sagaId);
                }
            }, cancellationToken);

            return Task.FromResult(sagaId);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error starting saga {typeof(TSaga).Name}";
            _logger.LogError(ex, errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    public async Task HandleEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        try
        {
            _logger.LogInformation("Handling event: {EventType} with Id: {EventId}",
                typeof(TEvent).Name, @event.Id);

            // Find all saga event handlers for this event type
            var handlerType = typeof(ISagaEventHandler<>).MakeGenericType(typeof(TEvent));
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                try
                {
                    var method = handlerType.GetMethod("HandleAsync");
                    if (method != null)
                    {
                        var context = new SagaContext
                        {
                            CorrelationId = @event.Id.ToString(),
                            CreatedAt = @event.CreatedDate
                        };

                        await (Task)method.Invoke(handler, new object[] { @event, context, cancellationToken })!;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling event {EventType} with handler {HandlerType}",
                        typeof(TEvent).Name, handler?.GetType().Name ?? "Unknown");
                    // Continue with other handlers
                }
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error handling event {typeof(TEvent).Name}";
            _logger.LogError(ex, errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    public async Task ProcessPendingSagasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Processing pending sagas");

            var pendingSagas = await _stateStore.GetPendingSagasAsync(cancellationToken);

            foreach (var sagaState in pendingSagas)
            {
                try
                {
                    // Check if saga has timed out
                    var sagaDefinition = GetSagaDefinition(sagaState.SagaName);
                    if (sagaDefinition != null)
                    {
                        var elapsed = DateTime.UtcNow - sagaState.CreatedAt;
                        if (elapsed > sagaDefinition.GlobalTimeout)
                        {
                            _logger.LogWarning("Saga {SagaId} has timed out", sagaState.SagaId);
                            sagaState.Status = SagaStatus.TimedOut;
                            await _stateStore.UpdateSagaStateAsync(sagaState, cancellationToken);
                            continue;
                        }
                    }

                    // Continue execution for running sagas
                    if (sagaState.Status == SagaStatus.Running)
                    {
                        await _orchestrator.ExecuteAsync(sagaState.SagaId, sagaState.Context, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing pending saga {SagaId}", sagaState.SagaId);
                }
            }
        }
        catch (Exception ex)
        {
            var errorMessage = "Error processing pending sagas";
            _logger.LogError(ex, errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    private ISagaDefinition? GetSagaDefinition(string sagaName)
    {
        var sagaDefinitions = _serviceProvider.GetServices<ISagaDefinition>();
        return sagaDefinitions.FirstOrDefault(s => s.SagaName == sagaName);
    }
}

/// <summary>
/// Background service for processing pending sagas
/// </summary>
public class SagaBackgroundService : BackgroundService
{
    private readonly ISagaManager _sagaManager;
    private readonly ILogger<SagaBackgroundService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(1);

    public SagaBackgroundService(
        ISagaManager sagaManager,
        ILogger<SagaBackgroundService> logger)
    {
        _sagaManager = sagaManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Saga background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _sagaManager.ProcessPendingSagasAsync(stoppingToken);
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in saga background service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Saga background service stopped");
    }
}