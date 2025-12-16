using BookingCare.Services.AI.Models.DTOs.Insights;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.AI.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/ai-insights")]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class AiInsightsController : BaseApiController
{
    private readonly IAiInsightsService _insightsService;
    private readonly ILogger<AiInsightsController> _logger;

    public AiInsightsController(
        IAiInsightsService insightsService,
        ILogger<AiInsightsController> logger)
    {
        _insightsService = insightsService;
        _logger = logger;
    }

    [HttpGet("health")]
    [AllowAnonymous]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "AI - Insights",
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("generate")]
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generate([FromBody] GenerateAiInsightRequest request)
    {
        var safeRequest = request ?? new GenerateAiInsightRequest();
        _logger.LogInformation("Generating AI insights for period {Period}", safeRequest.Period);

        var result = await _insightsService.GenerateAsync(
            safeRequest,
            HttpContext.RequestAborted);

        return Success(result, "AI insights generated successfully");
    }

    [HttpPost("generate-for-doctor")]
    [Authorize(Policy = "Role:Doctor")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateForDoctor([FromBody] GenerateAiInsightRequest request)
    {
        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            
            // Get doctor ID from account ID (you may need to inject a service to do this)
            // For now, we'll need to get it from the doctor service via gRPC or similar
            // This is a placeholder - you'll need to implement the actual lookup
            var safeRequest = request ?? new GenerateAiInsightRequest();
            _logger.LogInformation("Generating AI insights for doctor account {AccountId}, period {Period}", accountId, safeRequest.Period);

            // TODO: Get doctorId from accountId using doctor service
            // For now, throw NotImplementedException
            return StatusCode(StatusCodes.Status501NotImplemented, new { Message = "Doctor ID lookup not implemented yet. Please use generate-for-doctor/{doctorId} endpoint." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpPost("generate-for-doctor/{doctorId}")]
    [Authorize(Policy = "Role:Doctor,Staff,Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateForDoctorById(Guid doctorId, [FromBody] GenerateAiInsightRequest request)
    {
        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            
            // TODO: Verify that the authenticated doctor can access this doctorId
            // For Doctor role, verify doctorId matches their own ID
            // For Staff/Admin, allow any doctorId
            
            var safeRequest = request ?? new GenerateAiInsightRequest();
            _logger.LogInformation("Generating AI insights for doctor {DoctorId}, period {Period}", doctorId, safeRequest.Period);

            var result = await _insightsService.GenerateForDoctorAsync(
                doctorId,
                safeRequest,
                HttpContext.RequestAborted);

            return Success(result, "AI insights generated successfully for doctor");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpPost("generate-for-hospital")]
    [Authorize(Policy = "Role:Staff")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateForHospital([FromBody] GenerateAiInsightRequest request)
    {
        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            
            // Get hospital ID from account ID (you may need to inject a service to do this)
            var safeRequest = request ?? new GenerateAiInsightRequest();
            _logger.LogInformation("Generating AI insights for hospital account {AccountId}, period {Period}", accountId, safeRequest.Period);

            // TODO: Get hospitalId from accountId using hospital service
            // For now, throw NotImplementedException
            return StatusCode(StatusCodes.Status501NotImplemented, new { Message = "Hospital ID lookup not implemented yet. Please use generate-for-hospital/{hospitalId} endpoint." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpPost("generate-for-hospital/{hospitalId}")]
    [Authorize(Policy = "Role:Staff,Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateForHospitalById(Guid hospitalId, [FromBody] GenerateAiInsightRequest request)
    {
        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            
            // TODO: Verify that the authenticated staff can access this hospitalId
            // For Staff role, verify hospitalId matches their own hospital
            // For Admin, allow any hospitalId
            
            var safeRequest = request ?? new GenerateAiInsightRequest();
            _logger.LogInformation("Generating AI insights for hospital {HospitalId}, period {Period}", hospitalId, safeRequest.Period);

            var result = await _insightsService.GenerateForHospitalAsync(
                hospitalId,
                safeRequest,
                HttpContext.RequestAborted);

            return Success(result, "AI insights generated successfully for hospital");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}



