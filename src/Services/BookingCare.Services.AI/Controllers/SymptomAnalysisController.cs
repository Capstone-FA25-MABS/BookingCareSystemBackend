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
    /// Analyze symptoms and provide recommendations
    /// </summary>
    /// <param name="request">Symptom analysis request with user message and context</param>
    /// <returns>Analysis response with disease possibilities, questions, and recommendations</returns>
    /// <response code="200">Analysis completed successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("analyze")]
    [AllowAnonymous] // Allow non-authenticated users to use AI support
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

            _logger.LogInformation("Analyzing symptoms for session: {SessionId}, message: {Message}",
                request.SessionId, request.Message?.Substring(0, Math.Min(request.Message.Length, 50)));

            var result = await _symptomAnalysisService.AnalyzeSymptomsAsync(request);

            // Add user info if authenticated
            if (request.UserId.HasValue)
            {
                _logger.LogInformation("Symptom analysis completed for user: {UserId}", request.UserId.Value);
            }

            return Success(result, "Symptom analysis completed successfully");
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
    /// Get all conversation sessions for a user
    /// </summary>
    /// <param name="userId">User ID (optional, can be passed as query parameter)</param>
    /// <returns>List of conversation sessions</returns>
    [HttpGet("sessions")]
    [AllowAnonymous]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetUserSessions([FromQuery] Guid? userId)
    {
        try
        {
            var sessions = await _symptomAnalysisService.GetUserSessionsAsync(userId);

            return Success(sessions, "Sessions retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for user {UserId}: {Message}", userId, ex.Message);
            return StatusCode(500, new
            {
                success = false,
                message = $"Failed to retrieve sessions: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Delete a conversation session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Delete confirmation</returns>
    [HttpDelete("sessions/{sessionId}")]
    [AllowAnonymous]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteSession(Guid sessionId)
    {
        try
        {
            var deleted = await _symptomAnalysisService.DeleteSessionAsync(sessionId);

            if (!deleted)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Session not found",
                    timestamp = DateTime.UtcNow
                });
            }

            return Success(new { sessionId }, "Session deleted successfully");
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


