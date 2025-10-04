using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Models;

namespace BookingCare.Shared.Saga.Core;

/// <summary>
/// Base class for saga definitions
/// </summary>
public abstract class SagaDefinitionBase : ISagaDefinition
{
    private readonly List<ISagaStep> _steps = new();

    public abstract string SagaName { get; }
    public virtual TimeSpan GlobalTimeout => TimeSpan.FromMinutes(30);
    public IEnumerable<ISagaStep> Steps => _steps.OrderBy(s => s.Order);

    protected void AddStep(ISagaStep step)
    {
        _steps.Add(step);
    }

    protected void AddStep<TStep>() where TStep : class, ISagaStep, new()
    {
        _steps.Add(new TStep());
    }
}

/// <summary>
/// Base class for compensatable saga steps
/// </summary>
public abstract class CompensatableSagaStepBase : ICompensatableSagaStep
{
    public abstract string StepName { get; }
    public abstract int Order { get; }
    public virtual TimeSpan Timeout => TimeSpan.FromMinutes(5);

    public abstract Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default);
    public abstract Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default);

    protected virtual SagaStepResult Success(Dictionary<string, object>? outputData = null)
    {
        return SagaStepResult.Success(outputData);
    }

    protected virtual SagaStepResult Failure(string errorMessage, Exception? exception = null, bool shouldRetry = false, TimeSpan? retryDelay = null)
    {
        return SagaStepResult.Failure(errorMessage, exception, shouldRetry, retryDelay);
    }
}

/// <summary>
/// Base class for executable saga steps
/// </summary>
public abstract class ExecutableSagaStepBase : IExecutableSagaStep
{
    public abstract string StepName { get; }
    public abstract int Order { get; }
    public virtual TimeSpan Timeout => TimeSpan.FromMinutes(5);

    public abstract Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default);

    protected virtual SagaStepResult Success(Dictionary<string, object>? outputData = null)
    {
        return SagaStepResult.Success(outputData);
    }

    protected virtual SagaStepResult Failure(string errorMessage, Exception? exception = null, bool shouldRetry = false, TimeSpan? retryDelay = null)
    {
        return SagaStepResult.Failure(errorMessage, exception, shouldRetry, retryDelay);
    }
}