using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Services;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Common.Helpers;
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

    public UsersController(IUserService userService)
    {
        _userService = userService;
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
    /// Get current user profile (requires authentication)
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("profile")]
    [MapToApiVersion(ApiVersions.V1_0)]
    //[Authorize]
    public async Task<IActionResult> GetCurrentUserProfile()
    {
        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            var user = await _userService.GetByAccountIdAsync(accountId);
            if (user == null)
            {
                return NotFound("User profile not found");
            }

            return Success(user, "User profile retrieved successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Update current user profile (requires authentication)
    /// </summary>
    /// <param name="updateUserRequest">User update data</param>
    /// <returns>Updated user profile</returns>
    [HttpPut("profile")]
    [MapToApiVersion(ApiVersions.V1_0)]
    //[Authorize]
    public async Task<IActionResult> UpdateCurrentUserProfile([FromBody] UpdateUserRequest updateUserRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            // Get confirmation statuses from JWT claims
            var (emailConfirmed, phoneConfirmed) = JwtHelper.GetConfirmationStatuses(HttpContext);

            // Update with confirmation statuses (service will handle user lookup)
            var updatedUser = await _userService.UpdateByAccountIdAsync(accountId, updateUserRequest, emailConfirmed, phoneConfirmed);
            return Success(updatedUser, "User profile updated successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Get user by account ID (for internal use)
    /// </summary>
    /// <param name="accountId">Account ID</param>
    /// <returns>User details</returns>
    [HttpGet("account/{accountId:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
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
    /// Update user (Admin only - bypasses confirmation checks)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="updateUserRequest">User update data</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest updateUserRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        // Admin updates bypass confirmation checks (defaults to false, false)
        var user = await _userService.UpdateAsync(id, updateUserRequest, emailConfirmed: false, phoneConfirmed: false);
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

}