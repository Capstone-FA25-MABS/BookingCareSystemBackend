using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Saga.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class SagasController : BaseApiController
{
    private readonly ISagaStateStore _sagaStateStore;

    public SagasController(ISagaStateStore sagaStateStore)
    {
        _sagaStateStore = sagaStateStore;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult Health()
    {
        var healthData = new { Status = "Healthy", Service = "Saga", Timestamp = DateTime.UtcNow };
        return Success(healthData, "Saga service is healthy");
    }

    /// <summary>
    /// Get saga execution status
    /// </summary>
    /// <param name="sagaId">Saga ID</param>
    /// <returns>Saga status information</returns>
    [HttpGet("{sagaId}/status")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetSagaStatus(Guid sagaId)
    {
        try
        {
            var sagaState = await _sagaStateStore.GetSagaStateAsync(sagaId);

            if (sagaState == null)
            {
                return NotFound(new { Error = "Saga not found", SagaId = sagaId });
            }

            return Ok(new
            {
                SagaId = sagaId,
                Status = sagaState.Status.ToString(),
                Message = GetStatusMessage(sagaState.Status),
                CurrentStep = sagaState.CurrentStep,
                CompletedSteps = sagaState.CompletedSteps,
                CompensatedSteps = sagaState.CompensatedSteps,
                CreatedAt = sagaState.CreatedAt,
                UpdatedAt = sagaState.UpdatedAt,
                CompletedAt = sagaState.CompletedAt,
                ErrorMessage = sagaState.ErrorMessage,
                RetryCount = sagaState.RetryCount,
                NextRetryAt = sagaState.NextRetryAt
            });
        }
        catch (Exception ex)
        {
            return NotFound(new
            {
                SagaId = sagaId,
                Message = $"Saga not found or error retrieving status: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get status message for saga status
    /// </summary>
    private static string GetStatusMessage(SagaStatus status)
    {
        return status switch
        {
            SagaStatus.Pending => "Saga is pending execution",
            SagaStatus.Running => "Saga is currently running",
            SagaStatus.Completed => "Saga completed successfully",
            SagaStatus.Failed => "Saga execution failed",
            SagaStatus.Compensating => "Saga is compensating failed steps",
            SagaStatus.Compensated => "Saga compensation completed",
            SagaStatus.Cancelled => "Saga was cancelled",
            SagaStatus.TimedOut => "Saga execution timed out",
            _ => "Unknown saga status"
        };
    }



}