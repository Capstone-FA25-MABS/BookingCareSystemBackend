using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.FileUpload.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Appointment.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Authorize]
public class AttachmentController : BaseApiController
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<AttachmentController> _logger;

    public AttachmentController(
        IFileUploadService fileUploadService,
        ILogger<AttachmentController> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Upload appointment attachment (medical records, images, etc.)
    /// </summary>
    [HttpPost("upload")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UploadAttachment(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user account ID
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided");
            }

            // Validate file type (images, PDFs, Word documents)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Only image, PDF, and Word document files are allowed");
            }

            // Validate file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest("File size must not exceed 10MB");
            }

            // Upload to S3
            var request = new FileUploadRequest
            {
                FileStream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Folder = $"appointments/attachments",
                GenerateUniqueFileName = true
            };

            var result = await _fileUploadService.UploadFileAsync(request, cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Failed to upload attachment for account {AccountId}: {Error}",
                    accountId, result.ErrorMessage);
                return BadRequest(result);
            }

            _logger.LogInformation("Attachment uploaded successfully for account {AccountId}", accountId);

            return Success(result, "Attachment uploaded successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading attachment");
            return StatusCode(500, "An internal server error occurred");
        }
    }

    /// <summary>
    /// Delete appointment attachment
    /// </summary>
    [HttpDelete]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteAttachment(
        [FromQuery] string fileUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user account ID
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            if (string.IsNullOrEmpty(fileUrl))
            {
                return BadRequest("File URL is required");
            }

            // Extract S3 key from URL
            var s3Key = ExtractS3KeyFromUrl(fileUrl);
            if (string.IsNullOrEmpty(s3Key))
            {
                return BadRequest("Invalid file URL");
            }

            // Delete from S3
            var deleted = await _fileUploadService.DeleteFileAsync(s3Key, cancellationToken);
            if (!deleted)
            {
                _logger.LogWarning("Failed to delete attachment from S3: {S3Key}", s3Key);
                return BadRequest("Failed to delete attachment from storage");
            }

            _logger.LogInformation("Attachment deleted successfully for account {AccountId}", accountId);

            return Success(new { message = "Attachment deleted successfully" }, "Attachment deleted successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment");
            return StatusCode(500, "An internal server error occurred");
        }
    }

    /// <summary>
    /// Extract S3 key from full URL
    /// </summary>
    private static string? ExtractS3KeyFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath.TrimStart('/');

            // If URL contains bucket name in path, remove it
            var segments = path.Split('/');
            if (segments.Length > 2 && segments[0] != "appointments")
            {
                // Assuming first segment is bucket name, remove it
                path = string.Join("/", segments.Skip(1));
            }

            return path;
        }
        catch
        {
            return null;
        }
    }
}

