using BookingCare.Shared.Saga.Core;
using BookingCare.Shared.Saga.Models;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace BookingCare.Shared.Saga.Steps;

/// <summary>
/// Base class for gRPC saga steps to eliminate code duplication
/// </summary>
public abstract class BaseGrpcStep : CompensatableSagaStepBase
{
    protected readonly ILogger _logger;

    protected BaseGrpcStep(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle gRPC exceptions with consistent error handling
    /// </summary>
    protected SagaStepResult HandleGrpcException(Exception ex, string operation, string stepName)
    {
        return ex switch
        {
            RpcException rpcEx => HandleRpcException(rpcEx, operation, stepName),
            _ => HandleGenericException(ex, operation, stepName)
        };
    }

    /// <summary>
    /// Handle RPC exceptions with retry logic
    /// </summary>
    private SagaStepResult HandleRpcException(RpcException ex, string operation, string stepName)
    {
        _logger.LogError(ex, "[{StepName}] gRPC error {Operation}", stepName, operation);
        return Failure($"gRPC error: {ex.Status.Detail}", ex, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Handle generic exceptions
    /// </summary>
    private SagaStepResult HandleGenericException(Exception ex, string operation, string stepName)
    {
        _logger.LogError(ex, "[{StepName}] Unexpected error {Operation}", stepName, operation);
        return Failure($"Unexpected error: {ex.Message}", ex);
    }

    /// <summary>
    /// Log compensation start
    /// </summary>
    protected void LogCompensationStart(string stepName, Guid sagaId, string operation)
    {
        _logger.LogInformation("[{StepName}] Compensating - {Operation} for saga {SagaId}", stepName, operation, sagaId);
    }

    /// <summary>
    /// Log compensation warning for missing data
    /// </summary>
    protected SagaStepResult LogCompensationWarning(string stepName, string missingData)
    {
        _logger.LogWarning("[{StepName}] No {MissingData} found for compensation", stepName, missingData);
        return Success(); // Nothing to compensate
    }

    /// <summary>
    /// Handle successful gRPC response and store ID for compensation
    /// </summary>
    protected SagaStepResult HandleSuccessfulResponse<T>(T response, string idProperty, string emailProperty, string stepName, SagaContext context, string idKey)
    {
        var id = typeof(T).GetProperty(idProperty)?.GetValue(response)?.ToString();
        var email = typeof(T).GetProperty(emailProperty)?.GetValue(response)?.ToString();

        if (!string.IsNullOrEmpty(id))
        {
            context.SetData(idKey, id);
            _logger.LogInformation("[{StepName}] {StepName} created successfully: {{{IdKey}}}", stepName, stepName, idKey, id);
        }

        var result = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(id))
            result[idKey] = id;
        if (!string.IsNullOrEmpty(email))
            result[$"{idKey.Replace("Id", "Email")}"] = email;

        return Success(result);
    }
}
