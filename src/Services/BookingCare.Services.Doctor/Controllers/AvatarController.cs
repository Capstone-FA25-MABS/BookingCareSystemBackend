using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.FileUpload.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Doctor.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Authorize]
public class AvatarController : BaseApiController
{
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly IDoctorService _doctorService;
    private readonly ILogger<AvatarController> _logger;

    public AvatarController(
        FileUploadOrchestrator uploadOrchestrator,
        IDoctorService doctorService,
        ILogger<AvatarController> logger)
    {
        _uploadOrchestrator = uploadOrchestrator;
        _doctorService = doctorService;
        _logger = logger;
    }

    /// <summary>
    /// Upload doctor avatar
    /// </summary>
    [HttpPost("upload")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UploadAvatar(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            // Get current doctor to check for existing avatar
            var currentDoctor = await _doctorService.GetDoctorByAccountIdAsync(accountId);
            if (currentDoctor == null)
            {
                return NotFound("Doctor not found");
            }

            // Delete old avatar if exists (not default avatar)
            if (!string.IsNullOrEmpty(currentDoctor.AvatarUrl) &&
                currentDoctor.AvatarUrl != "https://bookingcaree.com/user-avatar-default.png")
            {
                var deleteConfig = new FileDeletionConfig
                {
                    FileUrl = currentDoctor.AvatarUrl,
                    ExpectedFolder = "avatars",
                    SuccessMessage = "Old avatar deleted successfully",
                    EntityType = "doctor-avatar"
                };

                var deleteResult = await _uploadOrchestrator.DeleteFileAsync(deleteConfig, accountId, _logger, cancellationToken);
                if (!deleteResult.Success)
                {
                    _logger.LogWarning("Failed to delete old avatar for account {AccountId}: {Error}", accountId, deleteResult.ErrorMessage);
                    // Continue with upload even if deletion fails
                }
            }

            var config = new FileUploadConfig
            {
                AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
                MaxSizeInMB = 5,
                Folder = "avatars/doctors",
                SuccessMessage = "Doctor avatar uploaded successfully",
                EntityType = "doctor-avatar"
            };

            var result = await _uploadOrchestrator.UploadFileAsync(file, config, accountId, _logger, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage!);
            }

            // Update doctor avatar URL in database - use CloudFront URL for public access
            var updateSuccess = await _doctorService.UpdateDoctorAvatarAsync(accountId, result.UploadResult?.CloudFrontUrl ?? result.UploadResult?.FileUrl ?? string.Empty);
            if (!updateSuccess)
            {
                _logger.LogWarning("Failed to update doctor avatar URL in database for account {AccountId}", accountId);
                // Don't fail the request since file was uploaded successfully
            }

            return Success(result.UploadResult, config.SuccessMessage);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Delete doctor avatar
    /// </summary>
    [HttpDelete]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken = default)
    {
        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            var doctor = await _doctorService.GetDoctorByAccountIdAsync(accountId);
            if (doctor == null)
            {
                return NotFound("Doctor not found");
            }

            if (string.IsNullOrEmpty(doctor.AvatarUrl) || doctor.AvatarUrl == "https://bookingcaree.com/user-avatar-default.png")
            {
                return BadRequest("Doctor has no custom avatar to delete");
            }

            var config = new FileDeletionConfig
            {
                FileUrl = doctor.AvatarUrl,
                ExpectedFolder = "avatars",
                SuccessMessage = "Doctor avatar deleted successfully",
                EntityType = "doctor-avatar"
            };

            var result = await _uploadOrchestrator.DeleteFileAsync(config, accountId, _logger, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage!);
            }

            // Update doctor avatar URL to default in database
            var updateSuccess = await _doctorService.UpdateDoctorAvatarAsync(accountId, "https://bookingcaree.com/user-avatar-default.png");
            if (!updateSuccess)
            {
                _logger.LogWarning("Failed to update doctor avatar URL to default in database for account {AccountId}", accountId);
                // Don't fail the request since file was deleted successfully
            }

            return Success(new { message = result.Message }, result.Message ?? "Doctor avatar deleted successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}
