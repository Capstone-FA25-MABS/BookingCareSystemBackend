using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.AI.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/symptoms")]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class SymptomAnalysisController : BaseApiController
{
    private readonly ISymptomAnalysisService _symptomAnalysisService;
    private readonly ILogger<SymptomAnalysisController> _logger;

    public SymptomAnalysisController(
        ISymptomAnalysisService symptomAnalysisService,
        ILogger<SymptomAnalysisController> logger)
    {
        _symptomAnalysisService = symptomAnalysisService;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "AI - Symptom Analysis",
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Analyze symptoms and provide recommendations (requires Patient authentication)
    /// </summary>
    /// <param name="request">Symptom analysis request with user message and context</param>
    /// <returns>Analysis response with disease possibilities, questions, and recommendations</returns>
    /// <response code="200">Analysis completed successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized - user must be authenticated</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("analyze")]
    [Authorize(Policy = "Role:Patient")] // Require Patient role
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(ApiResponse<SymptomAnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AnalyzeSymptoms([FromBody] SymptomAnalysisRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors,
                    timestamp = DateTime.UtcNow
                });
            }

            // Get authenticated user ID from JWT claims using JwtHelper
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            // Override request userId with authenticated user ID for security
            request.UserId = accountId;

            _logger.LogInformation("Analyzing symptoms for user: {UserId}, session: {SessionId}, message: {Message}",
                accountId, request.SessionId, request.Message?.Substring(0, Math.Min(request.Message.Length, 50)));

            var result = await _symptomAnalysisService.AnalyzeSymptomsAsync(request);

            _logger.LogInformation("Symptom analysis completed for user: {UserId}", accountId);

            return Success(result, "Symptom analysis completed successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt: {Message}", ex.Message);
            return Unauthorized(new
            {
                success = false,
                message = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
        catch (ApplicationException ex)
        {
            _logger.LogError(ex, "Application error during symptom analysis: {Message}. StackTrace: {StackTrace}",
                ex.Message, ex.StackTrace);
            _logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException?.Message);
            return StatusCode(500, new
            {
                success = false,
                message = $"An error occurred while analyzing symptoms: {ex.Message}",
                timestamp = DateTime.UtcNow,
                error = ex.InnerException?.Message ?? ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during symptom analysis: {Message}. StackTrace: {StackTrace}",
                ex.Message, ex.StackTrace);
            _logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException?.Message);
            return StatusCode(500, new
            {
                success = false,
                message = $"An unexpected error occurred: {ex.Message}",
                timestamp = DateTime.UtcNow,
                error = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    /// <summary>
    /// Get conversation session history
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Conversation history</returns>
    [HttpGet("sessions/{sessionId}")]
    [AllowAnonymous]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        try
        {
            var conversationHistory = await _symptomAnalysisService.GetConversationHistoryAsync(sessionId);

            return Success(new
            {
                sessionId,
                conversationHistory
            }, "Session retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}: {Message}", sessionId, ex.Message);
            return StatusCode(500, new
            {
                success = false,
                message = $"Failed to retrieve session: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get all conversation sessions for authenticated user
    /// </summary>
    /// <returns>List of conversation sessions</returns>
    [HttpGet("sessions")]
    [Authorize(Policy = "Role:Patient")] // Require Patient role
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetUserSessions()
    {
        try
        {
            // Get authenticated user ID from JWT claims using JwtHelper
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            var sessions = await _symptomAnalysisService.GetUserSessionsAsync(accountId);

            return Success(sessions, "Sessions retrieved successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions: {Message}", ex.Message);
            return StatusCode(500, new
            {
                success = false,
                message = $"Failed to retrieve sessions: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Delete a conversation session (requires authentication and ownership)
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Delete confirmation</returns>
    [HttpDelete("sessions/{sessionId}")]
    [Authorize(Policy = "Role:Patient")] // Require Patient role
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteSession(Guid sessionId)
    {
        try
        {
            // Get authenticated user ID from JWT claims using JwtHelper
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            var deleted = await _symptomAnalysisService.DeleteSessionAsync(sessionId, accountId);

            if (!deleted)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Session not found or you don't have permission to delete it",
                    timestamp = DateTime.UtcNow
                });
            }

            return Success(new { sessionId }, "Session deleted successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}: {Message}", sessionId, ex.Message);
            return StatusCode(500, new
            {
                success = false,
                message = $"Failed to delete session: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Save conversation session (for future implementation)
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="request">Session data to save</param>
    /// <returns>Save confirmation</returns>
    [HttpPost("sessions/{sessionId}/save")]
    [AllowAnonymous]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult SaveSession(Guid sessionId, [FromBody] object request)
    {
        // TODO: Implement session persistence to database
        return Ok(new
        {
            success = true,
            message = "Session persistence not yet implemented",
            data = new { sessionId }
        });
    }
}


