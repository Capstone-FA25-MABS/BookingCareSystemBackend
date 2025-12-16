using BookingCare.Services.AI.Models.DTOs;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.AI.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class AisController : BaseApiController
{
    private readonly IAIService _aiService;

    public AisController(IAIService aiService)
    {
        _aiService = aiService;
    }

    [HttpGet("health")]
    [AllowAnonymous]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(
            new
            {
                Status = "Healthy",
                Service = "AI",
                Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
                Timestamp = DateTime.UtcNow,
            }
        );
    }

    /// <summary>
    /// Generate medical summary from conversation transcript using AI
    /// </summary>
    /// <param name="request">Request containing conversation transcript</param>
    /// <returns>AI-generated medical summary for doctor review</returns>
    /// <response code="200">Medical summary generated successfully</response>
    /// <response code="400">Invalid request data or generation failed</response>
    /// <response code="401">Unauthorized - user must be authenticated as Doctor/Staff/Admin</response>
    [HttpPost("generate-medical-summary")]
    [Authorize(Policy = "Role:Doctor,Staff,Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateMedicalSummary(
        [FromBody] GenerateMedicalSummaryRequest request
    )
    {
        var result = await _aiService.GenerateMedicalSummaryAsync(request);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Success(result, "Medical summary generated successfully");
    }
}
