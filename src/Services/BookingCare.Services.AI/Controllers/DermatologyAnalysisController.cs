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

/// <summary>
/// Controller for dermatology image analysis
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/dermatology")]
[ApiVersion(ApiVersions.V1_0)]
public class DermatologyAnalysisController : BaseApiController
{
    private readonly IDermatologyAnalysisService _dermatologyService;
    private readonly ILogger<DermatologyAnalysisController> _logger;

    public DermatologyAnalysisController(
        IDermatologyAnalysisService dermatologyService,
        ILogger<DermatologyAnalysisController> logger)
    {
        _dermatologyService = dermatologyService;
        _logger = logger;
    }

    /// <summary>
    /// Analyze skin image for dermatological conditions (requires Patient authentication)
    /// </summary>
    /// <param name="file">Skin image file (jpg, png, etc.)</param>
    /// <param name="sessionId">Optional session ID for tracking</param>
    /// <param name="provinceId">Optional province ID for location-based recommendations</param>
    /// <param name="districtId">Optional district ID for location-based recommendations</param>
    /// <param name="locationDisplayName">Optional location display name</param>
    /// <returns>Dermatology analysis with diagnosis, malignancy assessment, and recommendations</returns>
    /// <response code="200">Analysis completed successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized - user must be authenticated as Patient</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("analyze")]
    [Authorize(Policy = "Role:Patient")] // Require Patient role
    [MapToApiVersion(ApiVersions.V1_0)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<DermatologyAnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(16 * 1024 * 1024)] // 16MB limit: 15MB file + 1MB buffer for multipart form overhead
    public async Task<IActionResult> AnalyzeSkinImage(
        [FromForm] IFormFile file,
        [FromForm] Guid? sessionId = null,
        [FromForm] string? provinceId = null,
        [FromForm] string? districtId = null,
        [FromForm] string? locationDisplayName = null)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "File is required" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new
                {
                    error = $"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}"
                });
            }

            // Validate file size (max 15MB for dermatology images)
            // Typical smartphone images are 2-5MB, but we allow up to 15MB for high-quality medical images
            const long maxFileSize = 15 * 1024 * 1024; // 15MB
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { error = "File size exceeds 15MB limit" });
            }

            // Get authenticated user ID from JWT claims using JwtHelper
            var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            // Build location context
            LocationContext? location = null;
            if (!string.IsNullOrEmpty(provinceId) && !string.IsNullOrEmpty(locationDisplayName))
            {
                location = new LocationContext
                {
                    ProvinceId = provinceId,
                    DistrictId = districtId,
                    DisplayName = locationDisplayName
                };
            }

            _logger.LogInformation(
                "Analyzing dermatology image for user {UserId}, session {SessionId}",
                userId, sessionId);

            var result = await _dermatologyService.AnalyzeSkinImageAsync(
                file,
                location,
                userId,
                sessionId);

            return Success(result, "Phân tích ảnh da thành công");
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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during dermatology analysis");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing dermatology image");
            return StatusCode(500, new
            {
                error = "An error occurred while analyzing the image. Please try again later."
            });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            service = "DermatologyAnalysis",
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }
}
