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
    protected void LogCompensationStart(string stepName, string sagaId, string operation)
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
}
