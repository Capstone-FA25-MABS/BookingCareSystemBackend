using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Enums;
using BookingCare.Shared.Common.Controllers;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Communication.Controllers;

/// <summary>
/// Enhanced FileUpload Controller with AWS S3 + CloudFront support
/// Provides new endpoints while maintaining backward compatibility
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/enhanced-fileupload")]
public class EnhancedFileUploadController : BaseApiController
{
    private readonly IHybridFileUploadService _hybridFileUploadService;
    private readonly ILogger<EnhancedFileUploadController> _logger;

    public EnhancedFileUploadController(
        IHybridFileUploadService hybridFileUploadService,
        ILogger<EnhancedFileUploadController> logger)
    {
        _hybridFileUploadService = hybridFileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Upload file to AWS S3 + CloudFront (NEW ENDPOINT)
    /// </summary>
    [HttpPost("s3/upload")]
    public async Task<IActionResult> UploadToS3(
        [FromForm] IFormFile file,
        [FromForm] string userId,
        [FromForm] MessageType messageType,
        [FromForm] string? customFolder = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File không ???c ?? tr?ng");
        }

        try
        {
            // Validate file first
            var validation = await _hybridFileUploadService.ValidateFileAsync(file, messageType);
            if (!validation.IsValid)
            {
                return BadRequest(new { Errors = validation.Errors });
            }

            // Upload to S3
            var result = await _hybridFileUploadService.UploadToS3Async(file, userId, messageType, customFolder);

            return Success(result, "Upload file to AWS S3 + CloudFront thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "L?i khi upload file to S3: {FileName}", file.FileName);
            return BadRequest("L?i khi upload file to S3");
        }
    }

    /// <summary>
    /// Upload file to Cloudinary (NEW ENDPOINT)
    /// </summary>
    [HttpPost("cloudinary/upload")]
    public async Task<IActionResult> UploadToCloudinary(
        [FromForm] IFormFile file,
        [FromForm] string userId,
        [FromForm] MessageType messageType)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File không ???c ?? tr?ng");
        }

        try
        {
            // Validate file first
            var validation = await _hybridFileUploadService.ValidateFileAsync(file, messageType);
            if (!validation.IsValid)
            {
                return BadRequest(new { Errors = validation.Errors });
            }

            // Upload to Cloudinary
            var result = await _hybridFileUploadService.UploadToCloudinaryAsync(file, userId, messageType);

            return Success(result, "Upload file to Cloudinary thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "L?i khi upload file to Cloudinary: {FileName}", file.FileName);
            return BadRequest("L?i khi upload file to Cloudinary");
        }
    }

    /// <summary>
    /// Smart upload - auto-routes to best storage provider (NEW ENDPOINT)
    /// </summary>
    [HttpPost("smart-upload")]
    public async Task<IActionResult> SmartUpload(
        [FromForm] IFormFile file,
        [FromForm] string userId,
        [FromForm] MessageType messageType)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File không ???c ?? tr?ng");
        }

        try
        {
            // Validate file first
            var validation = await _hybridFileUploadService.ValidateFileAsync(file, messageType);
            if (!validation.IsValid)
            {
                return BadRequest(new { Errors = validation.Errors });
            }

            // Smart upload with automatic routing
            var result = await _hybridFileUploadService.UploadFileAsync(file, userId, messageType);

            var provider = result.Url.Contains("cloudfront.net") || result.Url.Contains("amazonaws.com")
                ? "AWS S3 + CloudFront"
                : "Cloudinary";

            return Success(new
            {
                Result = result,
                Provider = provider,
                Routing = "Auto-routed based on MessageType and content"
            }, $"Smart upload thành công qua {provider}!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "L?i khi smart upload file: {FileName}", file.FileName);
            return BadRequest("L?i khi smart upload file");
        }
    }

    /// <summary>
    /// Get CloudFront URL for S3 key (NEW ENDPOINT)
    /// </summary>
    [HttpGet("s3/cloudfront-url")]
    public IActionResult GetCloudFrontUrl([FromQuery] string s3Key)
    {
        if (string.IsNullOrEmpty(s3Key))
        {
            return BadRequest("S3 key is required");
        }

        try
        {
            var cloudFrontUrl = _hybridFileUploadService.GetCloudFrontUrl(s3Key);

            return Success(new
            {
                S3Key = s3Key,
                CloudFrontUrl = cloudFrontUrl
            }, "CloudFront URL generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating CloudFront URL for S3 key: {S3Key}", s3Key);
            return BadRequest("Error generating CloudFront URL");
        }
    }

    /// <summary>
    /// Check if file exists in either storage (NEW ENDPOINT)
    /// </summary>
    [HttpHead("exists")]
    public async Task<IActionResult> CheckFileExists([FromQuery] string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl))
        {
            return BadRequest("File URL is required");
        }

        try
        {
            var exists = await _hybridFileUploadService.FileExistsAsync(fileUrl);

            return exists ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {FileUrl}", fileUrl);
            return BadRequest("Error checking file existence");
        }
    }

    /// <summary>
    /// Get file info (NEW ENDPOINT)
    /// </summary>
    [HttpGet("info")]
    public async Task<IActionResult> GetFileInfo([FromQuery] string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl))
        {
            return BadRequest("File URL is required");
        }

        try
        {
            var exists = await _hybridFileUploadService.FileExistsAsync(fileUrl);

            if (!exists)
            {
                return NotFound("File not found");
            }

            var provider = fileUrl.Contains("cloudfront.net") || fileUrl.Contains("amazonaws.com")
                ? "AWS S3 + CloudFront"
                : "Cloudinary";

            var info = new
            {
                Url = fileUrl,
                Provider = provider,
                Exists = true,
                CheckedAt = DateTime.UtcNow
            };

            return Success(info, "File info retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info: {FileUrl}", fileUrl);
            return BadRequest("Error getting file info");
        }
    }

    /// <summary>
    /// Bulk upload with smart routing (NEW ENDPOINT)
    /// </summary>
    [HttpPost("bulk-smart-upload")]
    public async Task<IActionResult> BulkSmartUpload([FromForm] BulkUploadRequest request)
    {
        if (request.Files == null || !request.Files.Any())
        {
            return BadRequest("Files are required");
        }

        try
        {
            var results = await _hybridFileUploadService.UploadMultipleFilesAsync(
                request.Files, request.UserId, request.MessageType);

            var summary = new
            {
                TotalFiles = request.Files.Count(),
                SuccessCount = results.Count,
                Results = results.Select(r => new
                {
                    r.Url,
                    r.FileName,
                    r.Size,
                    Provider = r.Url.Contains("cloudfront.net") || r.Url.Contains("amazonaws.com")
                        ? "AWS S3 + CloudFront"
                        : "Cloudinary"
                })
            };

            return Success(summary, $"Bulk upload completed: {results.Count} files uploaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk smart upload for user {UserId}", request.UserId);
            return BadRequest("Error in bulk upload");
        }
    }

    /// <summary>
    /// Health check for hybrid file upload service (NEW ENDPOINT)
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        var healthData = new
        {
            Status = "Healthy",
            Service = "Enhanced File Upload (Hybrid)",
            Version = "1.0",
            Providers = new[]
            {
                "AWS S3 + CloudFront",
                "Cloudinary"
            },
            Features = new[]
            {
                "Smart routing based on MessageType",
                "AWS S3 for documents and general files",
                "Cloudinary for rich media processing",
                "CloudFront CDN for global delivery",
                "Backward compatibility maintained"
            },
            Timestamp = DateTime.UtcNow
        };

        return Success(healthData, "Enhanced File Upload Service is healthy");
    }
}

/// <summary>
/// Request for bulk upload
/// </summary>
public class BulkUploadRequest
{
    [Required]
    [JsonRequired]
    public IFormFileCollection Files { get; set; } = null!;

    [Required]
    [JsonRequired]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [JsonRequired]
    public MessageType MessageType { get; set; }
}