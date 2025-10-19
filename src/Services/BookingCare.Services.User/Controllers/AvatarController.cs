using BookingCare.Services.User.Services;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
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
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly IUserService _userService;
    private readonly ILogger<AvatarController> _logger;

    public AvatarController(
        FileUploadOrchestrator uploadOrchestrator,
        IUserService userService,
        ILogger<AvatarController> logger)
    {
        _uploadOrchestrator = uploadOrchestrator;
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
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            var config = new FileUploadConfig
            {
                AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
                MaxSizeInMB = 5,
                Folder = "avatars/patients",
                SuccessMessage = "Avatar uploaded successfully",
                EntityType = "avatar"
            };

            var result = await _uploadOrchestrator.UploadFileAsync(file, config, accountId, _logger, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage!);
            }

            return Success(result.UploadResult, config.SuccessMessage);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
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

            var config = new FileDeletionConfig
            {
                FileUrl = user.AvatarUrl,
                ExpectedFolder = "avatars",
                SuccessMessage = "Avatar deleted successfully",
                EntityType = "avatar"
            };

            var result = await _uploadOrchestrator.DeleteFileAsync(config, accountId, _logger, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage!);
            }

            return Success(new { message = result.Message }, result.Message ?? "Avatar deleted successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}

