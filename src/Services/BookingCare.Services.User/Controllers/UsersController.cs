using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Services;
using BookingCare.Shared.Common.Controllers;
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
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IFileUploadService _fileUploadService;

    public UsersController(IUserService userService, IFileUploadService fileUploadService)
    {
        _userService = userService;
        _fileUploadService = fileUploadService;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult Health()
    {
        var healthData = new { Status = "Healthy", Service = "User", Timestamp = DateTime.UtcNow };
        return Success(healthData, "User service is healthy");
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        return Success(user, "User retrieved successfully");
    }

    /// <summary>
    /// Get user by account ID
    /// </summary>
    /// <param name="accountId">Account ID</param>
    /// <returns>User details</returns>
    [HttpGet("account/{accountId:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetUserByAccountId(Guid accountId)
    {
        var user = await _userService.GetByAccountIdAsync(accountId);
        if (user == null)
        {
            return NotFound($"User with account ID {accountId} not found");
        }

        return Success(user, "User retrieved successfully");
    }

    /// <summary>
    /// Update user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="updateUserRequest">User update data</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest updateUserRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var user = await _userService.UpdateAsync(id, updateUserRequest);
        return Success(user, "User updated successfully");
    }

    /// <summary>
    /// Get users with filtering and pagination
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetUsers([FromQuery] UserQueryRequest query)
    {
        var result = await _userService.GetUsersAsync(query);
        return Success(result, "Users retrieved successfully");
    }

    /// <summary>
    /// Search users by term
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of matching users</returns>
    [HttpGet("search")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest("Search term is required");
        }

        var users = await _userService.SearchUsersAsync(searchTerm, limit);
        return Success(users, "Users search completed successfully");
    }

    /// <summary>
    /// Upload user avatar
    /// </summary>
    /// <param name="file">Avatar image file</param>
    /// <returns>Upload result with URLs</returns>
    [HttpPost("upload-avatar")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        try
        {
            // Validate file input
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided or file is empty");
            }

            // Validate file size (10MB limit)
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                return BadRequest($"File size exceeds maximum allowed size of {maxFileSize / (1024 * 1024)}MB");
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return BadRequest($"File type '{file.ContentType}' is not allowed. Allowed types: {string.Join(", ", allowedTypes)}");
            }

            using var stream = file.OpenReadStream();

            var request = new FileUploadRequest
            {
                FileStream = stream,
                FileName = file.FileName,
                ContentType = file.ContentType,
                Folder = "avatars",
                GenerateUniqueFileName = true,
                Metadata = new Dictionary<string, string>
                {
                    ["UploadType"] = "Avatar",
                    ["OriginalFileName"] = file.FileName,
                    ["UploadedAt"] = DateTime.UtcNow.ToString("O")
                }
            };

            var result = await _fileUploadService.UploadFileAsync(request);

            if (result.Success)
            {
                return Success(new
                {
                    FileUrl = result.FileUrl,
                    CloudFrontUrl = result.CloudFrontUrl,
                    S3Key = result.S3Key,
                    FileName = result.FileName,
                    FileSize = result.FileSize,
                    ContentType = result.ContentType
                }, "Avatar uploaded successfully");
            }

            return BadRequest(result.ErrorMessage ?? "File upload failed");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while uploading the avatar: {ex.Message}");
        }
    }
}