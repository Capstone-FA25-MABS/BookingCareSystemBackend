using System.Text.Json;

namespace BookingCare.Shared.Saga.Models;

/// <summary>
/// Represents the context data passed throughout the saga execution
/// </summary>
public class SagaContext
{
    public Guid SagaId { get; set; }
    public string SagaName { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public string CorrelationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Headers { get; set; } = new();

    public T? GetData<T>(string key)
    {
        if (!Data.TryGetValue(key, out var value))
            return default;

        if (value is T directValue)
            return directValue;

        if (value is JsonElement jsonElement)
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());

        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<T>(json);
    }

    public void SetData<T>(string key, T value)
    {
        Data[key] = value!;
    }

    public bool HasData(string key) => Data.ContainsKey(key);
}

/// <summary>
/// Represents the result of a saga step execution
/// </summary>
public class SagaStepResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public Dictionary<string, object> OutputData { get; set; } = new();
    public bool ShouldRetry { get; set; }
    public TimeSpan? RetryDelay { get; set; }

    public static SagaStepResult Success(Dictionary<string, object>? outputData = null)
    {
        return new SagaStepResult
        {
            IsSuccess = true,
            OutputData = outputData ?? new Dictionary<string, object>()
        };
    }

    public static SagaStepResult Failure(string errorMessage, Exception? exception = null, bool shouldRetry = false, TimeSpan? retryDelay = null)
    {
        return new SagaStepResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            ShouldRetry = shouldRetry,
            RetryDelay = retryDelay
        };
    }
}

/// <summary>
/// Represents the overall result of saga execution
/// </summary>
public class SagaExecutionResult
{
    public Guid SagaId { get; set; }
    public bool IsSuccess { get; set; }
    public SagaStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public Dictionary<string, SagaStepResult> StepResults { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
}

/// <summary>
/// Represents the current state of a saga
/// </summary>
public class SagaState
{
    public Guid SagaId { get; set; }
    public string SagaName { get; set; } = string.Empty;
    public SagaStatus Status { get; set; }
    public SagaContext Context { get; set; } = new();
    public string CurrentStep { get; set; } = string.Empty;
    public List<string> CompletedSteps { get; set; } = new();
    public List<string> CompensatedSteps { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public Dictionary<string, object> StepData { get; set; } = new();
}

/// <summary>
/// Saga status enumeration
/// </summary>
public enum SagaStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Compensating,
    Compensated,
    Cancelled,
    TimedOut
}

/// <summary>
/// Saga step execution context
/// </summary>
public class SagaStepContext
{
    public string StepName { get; set; } = string.Empty;
    public SagaContext SagaContext { get; set; } = new();
    public Dictionary<string, object> StepData { get; set; } = new();
    public int AttemptNumber { get; set; } = 1;
    public bool IsCompensation { get; set; }
}