using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.FileUpload.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookingCare.Shared.FileUpload.Controllers;

/// <summary>
/// Base controller for avatar operations (upload/delete)
/// </summary>
[ApiController]
[Produces("application/json")]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Authorize]
public abstract class BaseAvatarController<TService, TController> : BaseApiController
    where TService : IAvatarService
    where TController : BaseAvatarController<TService, TController>
{
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly TService _avatarService;
    private readonly ILogger _logger;
    private readonly AvatarConfig _config;

    protected BaseAvatarController(
        FileUploadOrchestrator uploadOrchestrator,
        TService avatarService,
        ILogger logger,
        AvatarConfig config)
    {
        _uploadOrchestrator = uploadOrchestrator;
        _avatarService = avatarService;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Upload avatar
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

            // Check if entity exists
            var entityExists = await _avatarService.EntityExistsByAccountIdAsync(accountId);
            if (!entityExists)
            {
                return NotFound($"{_config.EntityDisplayName} not found");
            }

            // Get current avatar URL
            var currentAvatarUrl = await _avatarService.GetAvatarUrlByAccountIdAsync(accountId);

            // Delete old avatar if exists (not default avatar)
            if (!string.IsNullOrEmpty(currentAvatarUrl) &&
                currentAvatarUrl != _config.DefaultAvatarUrl)
            {
                var deleteConfig = new FileDeletionConfig
                {
                    FileUrl = currentAvatarUrl,
                    ExpectedFolder = "avatars",
                    SuccessMessage = "Old avatar deleted successfully",
                    EntityType = _config.EntityType
                };

                var deleteResult = await _uploadOrchestrator.DeleteFileAsync(
                    deleteConfig, accountId, _logger, cancellationToken);

                if (!deleteResult.Success)
                {
                    _logger.LogWarning(
                        "Failed to delete old avatar for account {AccountId}: {Error}",
                        accountId, deleteResult.ErrorMessage);
                    // Continue with upload even if deletion fails
                }
            }

            var uploadConfig = new FileUploadConfig
            {
                AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
                MaxSizeInMB = 5,
                Folder = _config.UploadFolder,
                SuccessMessage = _config.UploadSuccessMessage,
                EntityType = _config.EntityType
            };

            var result = await _uploadOrchestrator.UploadFileAsync(
                file, uploadConfig, accountId, _logger, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage!);
            }

            // Update avatar URL in database - use CloudFront URL for public access
            var avatarUrl = result.UploadResult?.CloudFrontUrl ??
                           result.UploadResult?.FileUrl ??
                           string.Empty;

            var updateSuccess = await _avatarService.UpdateAvatarUrlByAccountIdAsync(accountId, avatarUrl);

            if (!updateSuccess)
            {
                _logger.LogWarning(
                    "Failed to update {EntityName} avatar URL in database for account {AccountId}",
                    _config.EntityDisplayName, accountId);
                // Don't fail the request since file was uploaded successfully
            }

            return Success(result.UploadResult, uploadConfig.SuccessMessage);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Delete avatar
    /// </summary>
    [HttpDelete]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken = default)
    {
        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            // Check if entity exists
            var entityExists = await _avatarService.EntityExistsByAccountIdAsync(accountId);
            if (!entityExists)
            {
                return NotFound($"{_config.EntityDisplayName} not found");
            }

            var avatarUrl = await _avatarService.GetAvatarUrlByAccountIdAsync(accountId);

            if (string.IsNullOrEmpty(avatarUrl) || avatarUrl == _config.DefaultAvatarUrl)
            {
                return BadRequest($"{_config.EntityDisplayName} has no custom avatar to delete");
            }

            var deleteConfig = new FileDeletionConfig
            {
                FileUrl = avatarUrl,
                ExpectedFolder = "avatars",
                SuccessMessage = _config.DeleteSuccessMessage,
                EntityType = _config.EntityType
            };

            var result = await _uploadOrchestrator.DeleteFileAsync(
                deleteConfig, accountId, _logger, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage!);
            }

            // Update avatar URL to default in database
            var updateSuccess = await _avatarService.UpdateAvatarUrlByAccountIdAsync(
                accountId, _config.DefaultAvatarUrl);

            if (!updateSuccess)
            {
                _logger.LogWarning(
                    "Failed to update {EntityName} avatar URL to default in database for account {AccountId}",
                    _config.EntityDisplayName, accountId);
                // Don't fail the request since file was deleted successfully
            }

            return Success(new { message = result.Message },
                result.Message ?? _config.DeleteSuccessMessage);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}

/// <summary>
/// Configuration for avatar controller behavior
/// </summary>
public class AvatarConfig
{
    public required string EntityType { get; init; }
    public required string EntityDisplayName { get; init; }
    public required string UploadFolder { get; init; }
    public required string UploadSuccessMessage { get; init; }
    public required string DeleteSuccessMessage { get; init; }
    public string DefaultAvatarUrl { get; init; } = "https://bookingcaree.com/user-avatar-default.png";
}

