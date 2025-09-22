using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BookingCare.Shared.Saga.StateStore;

/// <summary>
/// In-memory implementation of saga state store (for development/testing)
/// </summary>
public class InMemorySagaStateStore : ISagaStateStore
{
    private readonly ConcurrentDictionary<Guid, SagaState> _sagaStates = new();
    private readonly ILogger<InMemorySagaStateStore> _logger;

    public InMemorySagaStateStore(ILogger<InMemorySagaStateStore> logger)
    {
        _logger = logger;
    }

    public Task<SagaState?> GetSagaStateAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting saga state for SagaId: {SagaId}", sagaId);

        var sagaState = _sagaStates.TryGetValue(sagaId, out var state) ? state : null;
        return Task.FromResult(sagaState);
    }

    public Task SaveSagaStateAsync(SagaState sagaState, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving saga state for SagaId: {SagaId}", sagaState.SagaId);

        var clonedState = CloneSagaState(sagaState);
        _sagaStates[sagaState.SagaId] = clonedState;
        return Task.CompletedTask;
    }

    public Task UpdateSagaStateAsync(SagaState sagaState, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating saga state for SagaId: {SagaId}", sagaState.SagaId);

        if (!_sagaStates.ContainsKey(sagaState.SagaId))
        {
            throw new InvalidOperationException($"Saga state not found for SagaId: {sagaState.SagaId}");
        }

        var clonedState = CloneSagaState(sagaState);
        _sagaStates[sagaState.SagaId] = clonedState;
        return Task.CompletedTask;
    }

    public Task DeleteSagaStateAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting saga state for SagaId: {SagaId}", sagaId);

        _sagaStates.TryRemove(sagaId, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<SagaState>> GetPendingSagasAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pending sagas");

        var pendingSagas = _sagaStates.Values
            .Where(s => s.Status == SagaStatus.Pending || s.Status == SagaStatus.Running)
            .Select(CloneSagaState)
            .ToList();

        return Task.FromResult<IEnumerable<SagaState>>(pendingSagas);
    }

    private SagaState CloneSagaState(SagaState original)
    {
        var json = JsonSerializer.Serialize(original);
        return JsonSerializer.Deserialize<SagaState>(json)!;
    }
}