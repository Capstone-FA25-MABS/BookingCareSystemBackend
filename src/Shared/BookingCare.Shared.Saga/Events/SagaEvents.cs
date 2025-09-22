using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Shared.Saga.Events;

/// <summary>
/// Base class for saga-related events
/// </summary>
public abstract class SagaEvent : IntegrationEvent
{
    public Guid SagaId { get; set; }
    public string SagaName { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Event published when a saga starts
/// </summary>
public class SagaStartedEvent : SagaEvent
{
    public string InitiatedBy { get; set; } = string.Empty;
    public Dictionary<string, object> InitialData { get; set; } = new();
}

/// <summary>
/// Event published when a saga completes successfully
/// </summary>
public class SagaCompletedEvent : SagaEvent
{
    public TimeSpan ExecutionTime { get; set; }
    public int CompletedSteps { get; set; }
    public Dictionary<string, object> FinalData { get; set; } = new();
}

/// <summary>
/// Event published when a saga fails
/// </summary>
public class SagaFailedEvent : SagaEvent
{
    public string ErrorMessage { get; set; } = string.Empty;
    public string FailedStep { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public int CompletedSteps { get; set; }
    public bool WillCompensate { get; set; }
}

/// <summary>
/// Event published when saga compensation starts
/// </summary>
public class SagaCompensationStartedEvent : SagaEvent
{
    public string Reason { get; set; } = string.Empty;
    public List<string> StepsToCompensate { get; set; } = new();
}

/// <summary>
/// Event published when saga compensation completes
/// </summary>
public class SagaCompensationCompletedEvent : SagaEvent
{
    public TimeSpan CompensationTime { get; set; }
    public int CompensatedSteps { get; set; }
    public List<string> FailedCompensations { get; set; } = new();
}

/// <summary>
/// Event published when a saga step completes
/// </summary>
public class SagaStepCompletedEvent : SagaEvent
{
    public string StepName { get; set; } = string.Empty;
    public TimeSpan StepExecutionTime { get; set; }
    public Dictionary<string, object> StepOutput { get; set; } = new();
}

/// <summary>
/// Event published when a saga step fails
/// </summary>
public class SagaStepFailedEvent : SagaEvent
{
    public string StepName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool WillRetry { get; set; }
    public int AttemptNumber { get; set; }
}

/// <summary>
/// Event published when a saga times out
/// </summary>
public class SagaTimedOutEvent : SagaEvent
{
    public TimeSpan TimeoutDuration { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public List<string> CompletedSteps { get; set; } = new();
}