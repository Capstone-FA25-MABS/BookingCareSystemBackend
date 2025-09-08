using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Models;
using Microsoft.Extensions.Logging;
using Polly;
using System.Diagnostics;

namespace BookingCare.Shared.Saga.Core;

/// <summary>
/// Default implementation of the saga orchestrator
/// </summary>
public class SagaOrchestrator : ISagaOrchestrator
{
    private readonly ISagaStateStore _stateStore;
    private readonly ILogger<SagaOrchestrator> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SagaOrchestrator(
        ISagaStateStore stateStore,
        ILogger<SagaOrchestrator> logger,
        IServiceProvider serviceProvider)
    {
        _stateStore = stateStore;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<SagaExecutionResult> ExecuteAsync(Guid sagaId, SagaContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SagaExecutionResult();

        try
        {
            _logger.LogInformation("Starting saga execution for SagaId: {SagaId}, SagaName: {SagaName}", 
                sagaId, context.SagaName);

            var sagaState = await _stateStore.GetSagaStateAsync(sagaId, cancellationToken);
            if (sagaState == null)
            {
                sagaState = new SagaState
                {
                    SagaId = sagaId,
                    SagaName = context.SagaName,
                    Status = SagaStatus.Running,
                    Context = context,
                    CreatedAt = DateTime.UtcNow
                };
                await _stateStore.SaveSagaStateAsync(sagaState, cancellationToken);
            }

            var sagaDefinition = GetSagaDefinition(context.SagaName);
            if (sagaDefinition == null)
            {
                throw new InvalidOperationException($"Saga definition not found for: {context.SagaName}");
            }

            result.TotalSteps = sagaDefinition.Steps.Count();

            foreach (var step in sagaDefinition.Steps)
            {
                if (sagaState.CompletedSteps.Contains(step.StepName))
                {
                    result.CompletedSteps++;
                    continue;
                }

                sagaState.CurrentStep = step.StepName;
                sagaState.UpdatedAt = DateTime.UtcNow;
                await _stateStore.UpdateSagaStateAsync(sagaState, cancellationToken);

                var stepResult = await ExecuteStepWithRetry(step, context, cancellationToken);
                result.StepResults[step.StepName] = stepResult;

                if (stepResult.IsSuccess)
                {
                    sagaState.CompletedSteps.Add(step.StepName);
                    result.CompletedSteps++;

                    // Merge step output data into saga context
                    foreach (var kvp in stepResult.OutputData)
                    {
                        context.SetData(kvp.Key, kvp.Value);
                    }
                }
                else
                {
                    _logger.LogError("Saga step failed: {StepName}, Error: {ErrorMessage}", 
                        step.StepName, stepResult.ErrorMessage);

                    sagaState.Status = SagaStatus.Failed;
                    sagaState.ErrorMessage = stepResult.ErrorMessage;
                    await _stateStore.UpdateSagaStateAsync(sagaState, cancellationToken);

                    result.IsSuccess = false;
                    result.Status = SagaStatus.Failed;
                    result.ErrorMessage = stepResult.ErrorMessage;
                    result.Exception = stepResult.Exception;

                    // Start compensation
                    await CompensateAsync(sagaId, context, cancellationToken);
                    
                    stopwatch.Stop();
                    result.ExecutionTime = stopwatch.Elapsed;
                    return result;
                }
            }

            sagaState.Status = SagaStatus.Completed;
            sagaState.CompletedAt = DateTime.UtcNow;
            await _stateStore.UpdateSagaStateAsync(sagaState, cancellationToken);

            result.IsSuccess = true;
            result.Status = SagaStatus.Completed;

            _logger.LogInformation("Saga execution completed successfully for SagaId: {SagaId}", sagaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga execution failed for SagaId: {SagaId}", sagaId);
            result.IsSuccess = false;
            result.Status = SagaStatus.Failed;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
        }

        stopwatch.Stop();
        result.ExecutionTime = stopwatch.Elapsed;
        return result;
    }

    public async Task<SagaExecutionResult> CompensateAsync(Guid sagaId, SagaContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SagaExecutionResult();

        try
        {
            _logger.LogInformation("Starting saga compensation for SagaId: {SagaId}", sagaId);

            var sagaState = await _stateStore.GetSagaStateAsync(sagaId, cancellationToken);
            if (sagaState == null)
            {
                throw new InvalidOperationException($"Saga state not found for SagaId: {sagaId}");
            }

            sagaState.Status = SagaStatus.Compensating;
            await _stateStore.UpdateSagaStateAsync(sagaState, cancellationToken);

            var sagaDefinition = GetSagaDefinition(context.SagaName);
            if (sagaDefinition == null)
            {
                throw new InvalidOperationException($"Saga definition not found for: {context.SagaName}");
            }

            // Compensate in reverse order
            var completedSteps = sagaDefinition.Steps
                .Where(s => sagaState.CompletedSteps.Contains(s.StepName))
                .OrderByDescending(s => s.Order);

            foreach (var step in completedSteps)
            {
                if (sagaState.CompensatedSteps.Contains(step.StepName))
                    continue;

                if (step is ICompensatableSagaStep compensatableStep)
                {
                    var stepResult = await CompensateStepWithRetry(compensatableStep, context, cancellationToken);
                    result.StepResults[step.StepName] = stepResult;

                    if (stepResult.IsSuccess)
                    {
                        sagaState.CompensatedSteps.Add(step.StepName);
                    }
                    else
                    {
                        _logger.LogError("Saga compensation step failed: {StepName}, Error: {ErrorMessage}", 
                            step.StepName, stepResult.ErrorMessage);
                        // Continue with other compensations even if one fails
                    }
                }
            }

            sagaState.Status = SagaStatus.Compensated;
            sagaState.CompletedAt = DateTime.UtcNow;
            await _stateStore.UpdateSagaStateAsync(sagaState, cancellationToken);

            result.IsSuccess = true;
            result.Status = SagaStatus.Compensated;

            _logger.LogInformation("Saga compensation completed for SagaId: {SagaId}", sagaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga compensation failed for SagaId: {SagaId}", sagaId);
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
        }

        stopwatch.Stop();
        result.ExecutionTime = stopwatch.Elapsed;
        return result;
    }

    public async Task<SagaStatus> GetSagaStatusAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        var sagaState = await _stateStore.GetSagaStateAsync(sagaId, cancellationToken);
        return sagaState?.Status ?? SagaStatus.Pending;
    }

    private async Task<SagaStepResult> ExecuteStepWithRetry(ISagaStep step, SagaContext context, CancellationToken cancellationToken)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, ctx) =>
                {
                    _logger.LogWarning("Retrying step {StepName}, attempt {RetryCount}", step.StepName, retryCount);
                });

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using var timeout = new CancellationTokenSource(step.Timeout);
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

            if (step is ICompensatableSagaStep compensatableStep)
            {
                return await compensatableStep.ExecuteAsync(context, combined.Token);
            }
            else if (step is IExecutableSagaStep executableStep)
            {
                return await executableStep.ExecuteAsync(context, combined.Token);
            }

            throw new NotSupportedException($"Step type not supported: {step.GetType().Name}");
        });
    }

    private async Task<SagaStepResult> CompensateStepWithRetry(ICompensatableSagaStep step, SagaContext context, CancellationToken cancellationToken)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, ctx) =>
                {
                    _logger.LogWarning("Retrying compensation for step {StepName}, attempt {RetryCount}", step.StepName, retryCount);
                });

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using var timeout = new CancellationTokenSource(step.Timeout);
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

            return await step.CompensateAsync(context, combined.Token);
        });
    }

    private ISagaDefinition? GetSagaDefinition(string sagaName)
    {
        var sagaDefinitionType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => typeof(ISagaDefinition).IsAssignableFrom(t) && 
                               !t.IsInterface && 
                               !t.IsAbstract);

        if (sagaDefinitionType == null)
            return null;

        return (ISagaDefinition?)Activator.CreateInstance(sagaDefinitionType);
    }
}
