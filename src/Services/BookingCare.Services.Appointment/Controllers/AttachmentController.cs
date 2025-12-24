using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.FileUpload.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Appointment.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Authorize(Policy = "Role:Admin,Staff,Doctor,Patient")]
public class AttachmentController : BaseApiController
{
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly ILogger<AttachmentController> _logger;

    public AttachmentController(
        FileUploadOrchestrator uploadOrchestrator,
        ILogger<AttachmentController> logger)
    {
        _uploadOrchestrator = uploadOrchestrator;
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
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            var config = new FileUploadConfig
            {
                AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" },
                MaxSizeInMB = 10,
                Folder = "appointments/attachments",
                SuccessMessage = "Attachment uploaded successfully",
                EntityType = "attachment"
            };

            var result = await _uploadOrchestrator.UploadFileAsync(file, config, accountId, _logger, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage!);
            }

            // Return CloudFront URL for better performance and public access
            // Fallback to FileUrl if CloudFront is not available
            var publicUrl = result.UploadResult?.CloudFrontUrl ??
                           result.UploadResult?.FileUrl ??
                           string.Empty;

            var response = new
            {
                fileUrl = publicUrl,
                fileName = result.UploadResult?.FileName,
                fileSize = result.UploadResult?.FileSize ?? 0,
                contentType = result.UploadResult?.ContentType,
                uploadedAt = result.UploadResult?.UploadedAt ?? DateTime.UtcNow
            };

            return Success(response, config.SuccessMessage);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
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
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            var config = new FileDeletionConfig
            {
                FileUrl = fileUrl,
                ExpectedFolder = "appointments",
                SuccessMessage = "Attachment deleted successfully",
                EntityType = "attachment"
            };

            var result = await _uploadOrchestrator.DeleteFileAsync(config, accountId, _logger, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage!);
            }

            return Success(new { message = result.Message }, result.Message ?? "Attachment deleted successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}

