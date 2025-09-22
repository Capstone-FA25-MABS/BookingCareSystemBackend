using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Saga.Models;

namespace BookingCare.Shared.Saga.Abstractions;

/// <summary>
/// Base interface for all saga steps
/// </summary>
public interface ISagaStep
{
    string StepName { get; }
    int Order { get; }
    TimeSpan Timeout { get; }
}

/// <summary>
/// Interface for saga steps that can be compensated
/// </summary>
public interface ICompensatableSagaStep : ISagaStep
{
    Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default);
    Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for saga steps that only execute forward
/// </summary>
public interface IExecutableSagaStep : ISagaStep
{
    Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for saga orchestrator
/// </summary>
public interface ISagaOrchestrator
{
    Task<SagaExecutionResult> ExecuteAsync(Guid sagaId, SagaContext context, CancellationToken cancellationToken = default);
    Task<SagaExecutionResult> CompensateAsync(Guid sagaId, SagaContext context, CancellationToken cancellationToken = default);
    Task<SagaStatus> GetSagaStatusAsync(Guid sagaId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for saga definition
/// </summary>
public interface ISagaDefinition
{
    string SagaName { get; }
    IEnumerable<ISagaStep> Steps { get; }
    TimeSpan GlobalTimeout { get; }
}

/// <summary>
/// Interface for saga state persistence
/// </summary>
public interface ISagaStateStore
{
    Task<SagaState?> GetSagaStateAsync(Guid sagaId, CancellationToken cancellationToken = default);
    Task SaveSagaStateAsync(SagaState sagaState, CancellationToken cancellationToken = default);
    Task UpdateSagaStateAsync(SagaState sagaState, CancellationToken cancellationToken = default);
    Task DeleteSagaStateAsync(Guid sagaId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SagaState>> GetPendingSagasAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for saga event handling
/// </summary>
public interface ISagaEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, SagaContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for saga manager
/// </summary>
public interface ISagaManager
{
    Task<Guid> StartSagaAsync<TSaga>(SagaContext context, CancellationToken cancellationToken = default)
        where TSaga : class, ISagaDefinition;

    Task HandleEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;

    Task ProcessPendingSagasAsync(CancellationToken cancellationToken = default);
}