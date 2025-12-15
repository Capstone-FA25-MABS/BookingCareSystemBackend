using BookingCare.Services.AI.Models.DTOs.Insights;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
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
}



