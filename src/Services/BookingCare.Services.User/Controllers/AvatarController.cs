using BookingCare.Services.User.Services;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.FileUpload.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.User.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Authorize]
public class AvatarController : BaseApiController
{
    private readonly IFileUploadService _fileUploadService;
    private readonly IUserService _userService;
    private readonly ILogger<AvatarController> _logger;

    public AvatarController(
        IFileUploadService fileUploadService,
        IUserService userService,
        ILogger<AvatarController> logger)
    {
        _fileUploadService = fileUploadService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Upload user avatar
    /// </summary>
    [HttpPost("upload")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UploadAvatar(
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

            // Validate file type (only images)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Only image files (jpg, jpeg, png, gif) are allowed");
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest("File size must not exceed 5MB");
            }

            // Upload to S3
            var request = new FileUploadRequest
            {
                FileStream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Folder = $"avatars/patients",
                GenerateUniqueFileName = true
            };

            var result = await _fileUploadService.UploadFileAsync(request, cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Failed to upload avatar for account {AccountId}: {Error}",
                    accountId, result.ErrorMessage);
                return BadRequest(result);
            }

            _logger.LogInformation("Avatar uploaded successfully for account {AccountId}", accountId);

            // Return upload result (URL will be saved when user clicks "Save Changes")
            return Success(result, "Avatar uploaded successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar");
            return StatusCode(500, "An internal server error occurred");
        }
    }

    /// <summary>
    /// Delete user avatar
    /// </summary>
    [HttpDelete]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user account ID
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            var user = await _userService.GetByAccountIdAsync(accountId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (string.IsNullOrEmpty(user.AvatarUrl))
            {
                return BadRequest("User has no avatar to delete");
            }

            // Extract S3 key from URL
            // Assuming URL format: https://cloudfront-domain.com/avatars/{accountId}/{filename}
            // or https://s3.region.amazonaws.com/bucket/avatars/{accountId}/{filename}
            var s3Key = ExtractS3KeyFromUrl(user.AvatarUrl);
            if (string.IsNullOrEmpty(s3Key))
            {
                return BadRequest("Invalid avatar URL");
            }

            // Delete from S3
            var deleted = await _fileUploadService.DeleteFileAsync(s3Key, cancellationToken);
            if (!deleted)
            {
                _logger.LogWarning("Failed to delete avatar from S3: {S3Key}", s3Key);
                return BadRequest("Failed to delete avatar from storage");
            }

            _logger.LogInformation("Avatar deleted successfully for account {AccountId}", accountId);

            // Return success (URL will be removed when user clicks "Save Changes")
            return Success(new { message = "Avatar deleted successfully" }, "Avatar deleted successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting avatar");
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
            // For CloudFront URL: https://cloudfront-domain.com/avatars/{accountId}/{filename}
            // For S3 URL: https://s3.region.amazonaws.com/bucket/avatars/{accountId}/{filename}

            var path = uri.AbsolutePath.TrimStart('/');

            // If URL contains bucket name in path, remove it
            var segments = path.Split('/');
            if (segments.Length > 2 && segments[0] != "avatars")
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

