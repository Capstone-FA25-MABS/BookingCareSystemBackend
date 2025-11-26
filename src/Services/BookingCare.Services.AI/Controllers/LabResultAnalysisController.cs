using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookingCare.Services.AI.Controllers;

/// <summary>
/// Controller for lab result analysis endpoints
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/lab-results")]
[ApiVersion(ApiVersions.V1_0)]
public class LabResultAnalysisController : BaseApiController
{
    private readonly ILabResultAnalysisService _labResultService;
    private readonly ILogger<LabResultAnalysisController> _logger;

    public LabResultAnalysisController(
        ILabResultAnalysisService labResultService,
        ILogger<LabResultAnalysisController> logger)
    {
        _labResultService = labResultService;
        _logger = logger;
    }

    /// <summary>
    /// Analyze lab result image and get medical insights
    /// </summary>
    /// <param name="request">Lab result analysis request with file</param>
    /// <returns>Analysis response with normal/abnormal indicators and recommendations</returns>
    [HttpPost("analyze")]
    [AllowAnonymous]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(ApiResponse<LabResultAnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<IActionResult> AnalyzeLabResult([FromForm] LabResultAnalysisRequest request)
    {
        try
        {
            // Validate file
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "File xét nghiệm là bắt buộc",
                    timestamp = DateTime.UtcNow
                });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Chỉ hỗ trợ file JPG, PNG, hoặc PDF",
                    timestamp = DateTime.UtcNow
                });
            }

            // Validate file size (10MB max)
            if (request.File.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Kích thước file không được vượt quá 10MB",
                    timestamp = DateTime.UtcNow
                });
            }

            // Get user ID from claims (if authenticated)
            Guid? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            _logger.LogInformation(
                "Analyzing lab result for user {UserId}, file: {FileName}, size: {Size} bytes",
                userId,
                request.File.FileName,
                request.File.Length);

            // Analyze lab result
            var response = await _labResultService.AnalyzeLabResultAsync(
                request.File,
                request.Location,
                userId,
                request.SessionId);

            return Success(response, "Phân tích kết quả xét nghiệm thành công");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during lab result analysis: {Message}", ex.Message);
            return BadRequest(new
            {
                success = false,
                message = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing lab result: {Message}", ex.Message);
            return StatusCode(500, new
            {
                success = false,
                message = "Đã có lỗi xảy ra khi phân tích kết quả xét nghiệm. Vui lòng thử lại sau.",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
