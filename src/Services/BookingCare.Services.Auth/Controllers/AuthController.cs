using AutoMapper;
using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Services;
using BookingCare.Services.Auth.Exceptions;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace BookingCare.Services.Auth.Controllers;

/// <summary>
/// Controller for handling authentication and authorization operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly CookieService _cookieService;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, CookieService cookieService)
    {
        _authService = authService;
        _logger = logger;
        _cookieService = cookieService;
    }

    #region Authentication Operations

    /// <summary>
    /// Authenticate account and generate JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var result = await _authService.LoginAsync(request);
        return Success(result, "Login successful");
    }

    /// <summary>
    /// Register new account
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {      
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var result = await _authService.RegisterAsync(request);
        return Created(result, "Account registered successfully");             
    }

    /// <summary>
    /// Refresh JWT token using refresh token
    /// </summary>
    /// <returns>New authentication response</returns>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = _cookieService.GetRefreshTokenFromCookies();
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized("Refresh token is missing in cookie");
        }

        var result = await _authService.RefreshTokenAsync(refreshToken);
        return Success(result, "Token refreshed successfully");
    }

    /// <summary>
    /// Logout account and invalidate refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token to invalidate</param>
    /// <returns>Success response</returns>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = _cookieService.GetRefreshTokenFromCookies();
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest("Refresh token is missing in cookie");
        }
        var result = await _authService.LogoutAsync(refreshToken);
        return Success("Logout successful");
    }

    /// <summary>
    /// Change account password
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <returns>Success response</returns>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {       
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var result = await _authService.ChangePasswordAsync(request);
        return Success("Password changed successfully");       
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    /// <param name="request">Password reset request</param>
    /// <returns>Success response</returns>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {      
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        // Ensure either Email or PhoneNumber is provided
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest("Either Email or PhoneNumber is required");
        }

        await _authService.ForgotPasswordAsync(request);

        // Do not reveal whether the target exists; return generic success
        var response = new {
            NextStep = !string.IsNullOrWhiteSpace(request.Email)
                ? "If the email exists, a reset link was sent"
                : "An OTP was sent if the phone is registered"
        };

        return Success(response, "If the account exists, instructions have been sent");      
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    /// <param name="request">Password reset with token request</param>
    /// <returns>Success response</returns>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {       
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var result = await _authService.ResetPasswordAsync(request);
        return Success("Password reset successfully");     
    }

    /// <summary>
    /// Issue reset token after OTP verification (phone flow)
    /// </summary>
    /// <param name="request">Issue reset token request</param>
    /// <returns>Reset token and URL</returns>
    [HttpPost("issue-reset-token")]
    public async Task<IActionResult> IssueResetToken([FromBody] IssueResetTokenRequest request)
    { 
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var result = await _authService.IssueResetTokenAsync(request);
        return Success(result, "Reset token issued if the phone is registered");     
    }

    #endregion

    

    #region Account Operations

    /// <summary>
    /// Create new account
    /// </summary>
    /// <param name="request">Account creation request</param>
    /// <returns>Created account information</returns>
    [HttpPost("accounts")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<AccountResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request data", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            var result = await _authService.CreateAccountAsync(request);
            return Created(result, "Account created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account for email: {Email}", request.Email);
            return BadRequest("Account creation failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Get account by ID
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Account information</returns>
    [HttpGet("accounts/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AccountResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetAccount(Guid id)
    {
        try
        {
            var account = await _authService.GetAccountByIdAsync(id);
            if (account == null)
            {
                return NotFound($"Account with ID {id} not found");
            }

            return Success(account, "Account retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by ID: {AccountId}", id);
            return BadRequest("Failed to get account: " + ex.Message);
        }
    }

    /// <summary>
    /// Get account by email
    /// </summary>
    /// <param name="email">Account email</param>
    /// <returns>Account information</returns>
    [HttpGet("accounts/by-email/{email}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AccountResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetAccountByEmail(string email)
    {
        try
        {
            var account = await _authService.GetAccountByEmailAsync(email);
            if (account == null)
            {
                return NotFound($"Account with email '{email}' not found");
            }

            return Success(account, "Account retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by email: {Email}", email);
            return BadRequest("Failed to get account: " + ex.Message);
        }
    }

    /// <summary>
    /// Get account by phone number
    /// </summary>
    /// <param name="phoneNumber">Account phone number</param>
    /// <returns>Account information</returns>
    [HttpGet("accounts/by-phone/{phoneNumber}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AccountResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetAccountByPhoneNumber(string phoneNumber)
    {
        try
        {
            var account = await _authService.GetAccountByPhoneNumberAsync(phoneNumber);
            if (account == null)
            {
                return NotFound($"Account with phone number '{phoneNumber}' not found");
            }

            return Success(account, "Account retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by phone number: {PhoneNumber}", phoneNumber);
            return BadRequest("Failed to get account: " + ex.Message);
        }
    }

    /// <summary>
    /// Update account information
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <param name="request">Account update request</param>
    /// <returns>Updated account information</returns>
    [HttpPut("accounts/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AccountResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountRequest request)
    {
        try
        {
            if (id != request.Id)
            {
                return BadRequest("ID mismatch between route and request body");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request data", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            var result = await _authService.UpdateAccountAsync(request);
            return Success(result, "Account updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account: {AccountId}", id);
            return BadRequest("Account update failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Delete account
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Success response</returns>
    [HttpDelete("accounts/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        try
        {
            var result = await _authService.DeleteAccountAsync(id);
            if (!result)
            {
                return NotFound($"Account with ID {id} not found");
            }

            return Success("Account deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account: {AccountId}", id);
            return BadRequest("Account deletion failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Get accounts with filtering and pagination
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <returns>Paginated list of accounts</returns>
    [HttpGet("accounts")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<AccountListResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetAccounts([FromQuery] AccountQueryRequest query)
    {
        try
        {
            var result = await _authService.GetAccountsAsync(query);
            return Success(result, "Accounts retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts with query");
            return BadRequest("Failed to get accounts: " + ex.Message);
        }
    }

    /// <summary>
    /// Activate account
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Success response</returns>
    [HttpPatch("accounts/{id}/activate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> ActivateAccount(Guid id)
    {
        try
        {
            var result = await _authService.ActivateAccountAsync(id);
            if (!result)
            {
                return NotFound($"Account with ID {id} not found");
            }

            return Success("Account activated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating account: {AccountId}", id);
            return BadRequest("Account activation failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Deactivate account
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Success response</returns>
    [HttpPatch("accounts/{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeactivateAccount(Guid id)
    {
        try
        {
            var result = await _authService.DeactivateAccountAsync(id);
            if (!result)
            {
                return NotFound($"Account with ID {id} not found");
            }

            return Success("Account deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating account: {AccountId}", id);
            return BadRequest("Account deactivation failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Lock account
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Success response</returns>
    [HttpPatch("accounts/{id}/lock")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> LockAccount(Guid id)
    {
        try
        {
            var result = await _authService.LockAccountAsync(id);
            if (!result)
            {
                return NotFound($"Account with ID {id} not found");
            }

            return Success("Account locked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking account: {AccountId}", id);
            return BadRequest("Account lock failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Unlock account
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Success response</returns>
    [HttpPatch("accounts/{id}/unlock")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UnlockAccount(Guid id)
    {
        try
        {
            var result = await _authService.UnlockAccountAsync(id);
            if (!result)
            {
                return NotFound($"Account with ID {id} not found");
            }

            return Success("Account unlocked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking account: {AccountId}", id);
            return BadRequest("Account unlock failed: " + ex.Message);
        }
    }

    #endregion

    #region Role Operations

    /// <summary>
    /// Create new role
    /// </summary>
    /// <param name="request">Role creation request</param>
    /// <returns>Created role information</returns>
    [HttpPost("roles")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request data", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            var result = await _authService.CreateRoleAsync(request);
            return Created(result, "Role created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role: {RoleName}", request.Name);
            return BadRequest("Role creation failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <returns>Role information</returns>
    [HttpGet("roles/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetRole(Guid id)
    {
        try
        {
            var role = await _authService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound($"Role with ID {id} not found");
            }

            return Success(role, "Role retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role by ID: {RoleId}", id);
            return BadRequest("Failed to get role: " + ex.Message);
        }
    }

    /// <summary>
    /// Get role by name
    /// </summary>
    /// <param name="name">Role name</param>
    /// <returns>Role information</returns>
    [HttpGet("roles/by-name/{name}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetRoleByName(string name)
    {
        try
        {
            var role = await _authService.GetRoleByNameAsync(name);
            if (role == null)
            {
                return NotFound($"Role with name '{name}' not found");
            }

            return Success(role, "Role retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role by name: {RoleName}", name);
            return BadRequest("Failed to get role: " + ex.Message);
        }
    }

    /// <summary>
    /// Update role information
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="request">Role update request</param>
    /// <returns>Updated role information</returns>
    [HttpPut("roles/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            if (id != request.Id)
            {
                return BadRequest("ID mismatch between route and request body");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request data", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            var result = await _authService.UpdateRoleAsync(request);
            return Success(result, "Role updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role: {RoleId}", id);
            return BadRequest("Role update failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Delete role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <returns>Success response</returns>
    [HttpDelete("roles/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        try
        {
            var result = await _authService.DeleteRoleAsync(id);
            if (!result)
            {
                return NotFound($"Role with ID {id} not found");
            }

            return Success("Role deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role: {RoleId}", id);
            return BadRequest("Role deletion failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Get roles with filtering and pagination
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <returns>Paginated list of roles</returns>
    [HttpGet("roles")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<RoleListResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetRoles([FromQuery] RoleQueryRequest query)
    {
        try
        {
            var result = await _authService.GetRolesAsync(query);
            return Success(result, "Roles retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles with query");
            return BadRequest("Failed to get roles: " + ex.Message);
        }
    }

    #endregion

    #region Permission Operations

    /// <summary>
    /// Create new permission
    /// </summary>
    /// <param name="request">Permission creation request</param>
    /// <returns>Created permission information</returns>
    [HttpPost("permissions")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PermissionResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request data", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            var result = await _authService.CreatePermissionAsync(request);
            return Created(result, "Permission created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission: {PermissionName}", request.Name);
            return BadRequest("Permission creation failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Get permission by ID
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <returns>Permission information</returns>
    [HttpGet("permissions/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PermissionResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetPermission(Guid id)
    {
        try
        {
            var permission = await _authService.GetPermissionByIdAsync(id);
            if (permission == null)
            {
                return NotFound($"Permission with ID {id} not found");
            }

            return Success(permission, "Permission retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission by ID: {PermissionId}", id);
            return BadRequest("Failed to get permission: " + ex.Message);
        }
    }

    /// <summary>
    /// Get permission by name
    /// </summary>
    /// <param name="name">Permission name</param>
    /// <returns>Permission information</returns>
    [HttpGet("permissions/by-name/{name}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PermissionResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetPermissionByName(string name)
    {
        try
        {
            var permission = await _authService.GetPermissionByNameAsync(name);
            if (permission == null)
            {
                return NotFound($"Permission with name '{name}' not found");
            }

            return Success(permission, "Permission retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission by name: {PermissionName}", name);
            return BadRequest("Failed to get permission: " + ex.Message);
        }
    }

    /// <summary>
    /// Update permission information
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <param name="request">Permission update request</param>
    /// <returns>Updated permission information</returns>
    [HttpPut("permissions/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PermissionResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> UpdatePermission(Guid id, [FromBody] UpdatePermissionRequest request)
    {
        try
        {
            if (id != request.Id)
            {
                return BadRequest("ID mismatch between route and request body");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request data", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            var result = await _authService.UpdatePermissionAsync(request);
            return Success(result, "Permission updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission: {PermissionId}", id);
            return BadRequest("Permission update failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Delete permission
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <returns>Success response</returns>
    [HttpDelete("permissions/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeletePermission(Guid id)
    {
        try
        {
            var result = await _authService.DeletePermissionAsync(id);
            if (!result)
            {
                return NotFound($"Permission with ID {id} not found");
            }

            return Success("Permission deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission: {PermissionId}", id);
            return BadRequest("Permission deletion failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Get permissions with filtering and pagination
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <returns>Paginated list of permissions</returns>
    [HttpGet("permissions")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PermissionListResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetPermissions([FromQuery] PermissionQueryRequest query)
    {
        try
        {
            var result = await _authService.GetPermissionsAsync(query);
            return Success(result, "Permissions retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions with query");
            return BadRequest("Failed to get permissions: " + ex.Message);
        }
    }

    #endregion

    #region Account-Role Operations

    /// <summary>
    /// Assign role to account
    /// </summary>
    /// <param name="request">Role assignment request</param>
    /// <returns>Account-role relationship information</returns>
    [HttpPost("accounts/assign-role")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<AccountRoleResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> AssignRoleToAccount([FromBody] AssignRoleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request data", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            var result = await _authService.AssignRoleToAccountAsync(request);
            return Success(result, "Role assigned to account successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to account: AccountId={AccountId}, RoleId={RoleId}", request.AccountId, request.RoleId);
            return BadRequest("Role assignment failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Remove role from account
    /// </summary>
    /// <param name="request">Role removal request</param>
    /// <returns>Success response</returns>
    [HttpDelete("accounts/remove-role")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> RemoveRoleFromAccount([FromBody] RemoveRoleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request data", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            var result = await _authService.RemoveRoleFromAccountAsync(request);
            return Success("Role removed from account successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from account: AccountId={AccountId}, RoleId={RoleId}", request.AccountId, request.RoleId);
            return BadRequest("Role removal failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Get account roles
    /// </summary>
    /// <param name="accountId">Account ID</param>
    /// <returns>List of roles assigned to account</returns>
    [HttpGet("accounts/{accountId}/roles")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<RoleResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetAccountRoles(Guid accountId)
    {
        try
        {
            var result = await _authService.GetAccountRolesAsync(accountId);
            return Success(result, "Account roles retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account roles: {AccountId}", accountId);
            return BadRequest("Failed to get account roles: " + ex.Message);
        }
    }

    /// <summary>
    /// Get accounts by role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <returns>List of accounts with specified role</returns>
    [HttpGet("roles/{roleId}/accounts")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<AccountResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetAccountsByRole(Guid roleId)
    {
        try
        {
            var result = await _authService.GetAccountsByRoleAsync(roleId);
            return Success(result, "Accounts by role retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts by role: {RoleId}", roleId);
            return BadRequest("Failed to get accounts by role: " + ex.Message);
        }
    }

    #endregion

    #region Role-Permission Operations

    /// <summary>
    /// Assign permission to role
    /// </summary>
    /// <param name="request">Permission assignment request</param>
    /// <returns>Role-permission relationship information</returns>
    [HttpPost("roles/assign-permission")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<RolePermissionResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> AssignPermissionToRole([FromBody] AssignPermissionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request data", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            var result = await _authService.AssignPermissionToRoleAsync(request);
            return Success(result, "Permission assigned to role successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission to role: RoleId={RoleId}, PermissionId={PermissionId}", request.RoleId, request.PermissionId);
            return BadRequest("Permission assignment failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Remove permission from role
    /// </summary>
    /// <param name="request">Permission removal request</param>
    /// <returns>Success response</returns>
    [HttpDelete("roles/remove-permission")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> RemovePermissionFromRole([FromBody] RemovePermissionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request data", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            var result = await _authService.RemovePermissionFromRoleAsync(request);
            return Success("Permission removed from role successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission from role: RoleId={RoleId}, PermissionId={PermissionId}", request.RoleId, request.PermissionId);
            return BadRequest("Permission removal failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Get role permissions
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <returns>List of permissions assigned to role</returns>
    [HttpGet("roles/{roleId}/permissions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<PermissionResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetRolePermissions(Guid roleId)
    {
        try
        {
            var result = await _authService.GetRolePermissionsAsync(roleId);
            return Success(result, "Role permissions retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role permissions: {RoleId}", roleId);
            return BadRequest("Failed to get role permissions: " + ex.Message);
        }
    }

    /// <summary>
    /// Get roles by permission
    /// </summary>
    /// <param name="permissionId">Permission ID</param>
    /// <returns>List of roles with specified permission</returns>
    [HttpGet("permissions/{permissionId}/roles")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<RoleResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetRolesByPermission(Guid permissionId)
    {
        try
        {
            var result = await _authService.GetRolesByPermissionAsync(permissionId);
            return Success(result, "Roles by permission retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles by permission: {PermissionId}", permissionId);
            return BadRequest("Failed to get roles by permission: " + ex.Message);
        }
    }

    #endregion

    #region Authorization Operations

    /// <summary>
    /// Check if account is authorized for specific permission
    /// </summary>
    /// <param name="accountId">Account ID</param>
    /// <param name="permissionName">Permission name</param>
    /// <returns>Authorization result</returns>
    [HttpGet("accounts/{accountId}/authorized/{permissionName}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> IsAuthorized(Guid accountId, string permissionName)
    {
        try
        {
            var result = await _authService.IsAuthorizedAsync(accountId, permissionName);
            return Success(result, result ? "Account is authorized" : "Account is not authorized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authorization: AccountId={AccountId}, Permission={Permission}", accountId, permissionName);
            return BadRequest("Authorization check failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Get all permissions for an account
    /// </summary>
    /// <param name="accountId">Account ID</param>
    /// <returns>List of permission names</returns>
    [HttpGet("accounts/{accountId}/permissions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetAccountPermissions(Guid accountId)
    {
        try
        {
            var result = await _authService.GetAccountPermissionsAsync(accountId);
            return Success(result, "Account permissions retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account permissions: {AccountId}", accountId);
            return BadRequest("Failed to get account permissions: " + ex.Message);
        }
    }

    #endregion

    #region Health Check

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult Health()
    {
        var healthData = new { Status = "Healthy", Service = "Auth", Timestamp = DateTime.UtcNow };
        return Success(healthData, "Auth service is healthy");
    }

    #endregion

    #region Middleware Testing

    /// <summary>
    /// Test endpoint to verify AuthenticationMiddleware and RefreshTokenMiddleware
    /// This endpoint requires authentication and will show user context from middleware
    /// </summary>
    /// <returns>User context information from middleware</returns>
    [HttpGet("test-middleware")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public IActionResult TestMiddleware()
    {
        try
        {
            // Get user context from AuthenticationMiddleware
            var userId = HttpContext.Items["UserId"]?.ToString();
            var userEmail = HttpContext.Items["UserEmail"]?.ToString();
            var userName = HttpContext.Items["UserName"]?.ToString();
            var userRoles = HttpContext.Items["UserRoles"] as List<string>;
            var userPermissions = HttpContext.Items["UserPermissions"] as List<string>;
            var accountStatus = HttpContext.Items["AccountStatus"]?.ToString();
            var jwtId = HttpContext.Items["JwtId"]?.ToString();

            // Get request headers for analysis
            var authorizationHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
            var xForwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var xRealIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();

            // Get response headers (set by RefreshTokenMiddleware if token was refreshed)
            var newAccessToken = HttpContext.Response.Headers["X-New-Access-Token"].FirstOrDefault();
            var tokenRefreshed = !string.IsNullOrEmpty(newAccessToken);

            var middlewareTestData = new
            {
                // AuthenticationMiddleware results
                AuthenticationMiddleware = new
                {
                    IsAuthenticated = !string.IsNullOrEmpty(userId),
                    UserId = userId,
                    UserEmail = userEmail,
                    UserName = userName,
                    UserRoles = userRoles ?? new List<string>(),
                    UserPermissions = userPermissions ?? new List<string>(),
                    AccountStatus = accountStatus,
                    JwtId = jwtId
                },

                // RefreshTokenMiddleware results
                RefreshTokenMiddleware = new
                {
                    TokenRefreshed = tokenRefreshed,
                    NewAccessToken = tokenRefreshed ? "Token was refreshed" : "No refresh needed"
                },

                // Request information
                RequestInfo = new
                {
                    AuthorizationHeader = authorizationHeader?.Substring(0, Math.Min(20, authorizationHeader.Length)) + "...",
                    UserAgent = userAgent,
                    ClientIp = xRealIp ?? xForwardedFor ?? HttpContext.Connection.RemoteIpAddress?.ToString(),
                    RequestTime = DateTime.UtcNow
                },

                // Middleware pipeline status
                MiddlewareStatus = new
                {
                    AuthenticationMiddlewareExecuted = !string.IsNullOrEmpty(userId),
                    RefreshTokenMiddlewareExecuted = true, // Always executed
                    GlobalExceptionFilterActive = true
                }
            };

            return Success(middlewareTestData, "Middleware test completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in middleware test endpoint");
            return BadRequest("Middleware test failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Test endpoint that requires specific permission
    /// This will test the permission claims from AuthenticationMiddleware
    /// </summary>
    /// <returns>Permission test result</returns>
    [HttpGet("test-permission")]
    [Authorize(Policy = "Perm:Content.Read")] // Requires Content.Read permission
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public IActionResult TestPermission()
    {
        try
        {
            var userPermissions = HttpContext.Items["UserPermissions"] as List<string>;
            var userRoles = HttpContext.Items["UserRoles"] as List<string>;

            var permissionTestData = new
            {
                UserPermissions = userPermissions ?? new List<string>(),
                UserRoles = userRoles ?? new List<string>(),
                HasAnyPermission = (userPermissions?.Count ?? 0) > 0,
                HasAnyRole = (userRoles?.Count ?? 0) > 0,
                TestMessage = "Permission test endpoint accessed successfully"
            };

            return Success(permissionTestData, "Permission test completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in permission test endpoint");
            return BadRequest("Permission test failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Test endpoint for refresh token functionality
    /// This endpoint will help test the RefreshTokenMiddleware behavior
    /// </summary>
    /// <returns>Refresh token test result</returns>
    [HttpGet("test-refresh-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public IActionResult TestRefreshToken()
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var jwtId = HttpContext.Items["JwtId"]?.ToString();
            var newAccessToken = HttpContext.Response.Headers["X-New-Access-Token"].FirstOrDefault();

            var refreshTokenTestData = new
            {
                UserId = userId,
                JwtId = jwtId,
                TokenRefreshed = !string.IsNullOrEmpty(newAccessToken),
                RefreshTokenMiddlewareStatus = "Active",
                TestMessage = "Refresh token test endpoint accessed successfully"
            };

            return Success(refreshTokenTestData, "Refresh token test completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in refresh token test endpoint");
            return BadRequest("Refresh token test failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Test endpoint that requires Admin role
    /// </summary>
    /// <returns>Admin role test result</returns>
    [HttpGet("test-role-admin")]
    [Authorize(Policy = "AdminOnly")] // Requires Admin role
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public IActionResult TestRoleAdmin()
    {
        try
        {
            var userContext = new
            {
                UserId = HttpContext.Items["UserId"]?.ToString(),
                UserEmail = HttpContext.Items["UserEmail"]?.ToString(),
                UserRoles = HttpContext.Items["UserRoles"] as List<string> ?? new List<string>(),
                UserPermissions = HttpContext.Items["UserPermissions"] as List<string> ?? new List<string>()
            };

            var roleTestData = new
            {
                UserContext = userContext,
                RequiredRole = "Admin",
                AccessGranted = true,
                TestMessage = "Admin role test endpoint accessed successfully"
            };

            return Success(roleTestData, "Admin role authorization test passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in admin role test endpoint");
            return BadRequest("Admin role test failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Test endpoint that requires Doctor role
    /// </summary>
    /// <returns>Doctor role test result</returns>
    [HttpGet("test-role-doctor")]
    [Authorize(Policy = "DoctorOnly")] // Requires Doctor role
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public IActionResult TestRoleDoctor()
    {
        try
        {
            var userContext = new
            {
                UserId = HttpContext.Items["UserId"]?.ToString(),
                UserEmail = HttpContext.Items["UserEmail"]?.ToString(),
                UserRoles = HttpContext.Items["UserRoles"] as List<string> ?? new List<string>(),
                UserPermissions = HttpContext.Items["UserPermissions"] as List<string> ?? new List<string>()
            };

            var roleTestData = new
            {
                UserContext = userContext,
                RequiredRole = "Doctor",
                AccessGranted = true,
                TestMessage = "Doctor role test endpoint accessed successfully"
            };

            return Success(roleTestData, "Doctor role authorization test passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in doctor role test endpoint");
            return BadRequest("Doctor role test failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Test endpoint that requires Content.Write permission
    /// </summary>
    /// <returns>Content write permission test result</returns>
    [HttpGet("test-permission-write")]
    [Authorize(Policy = "CanWriteContent")] // Requires Content.Write permission
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public IActionResult TestPermissionWrite()
    {
        try
        {
            var userContext = new
            {
                UserId = HttpContext.Items["UserId"]?.ToString(),
                UserEmail = HttpContext.Items["UserEmail"]?.ToString(),
                UserRoles = HttpContext.Items["UserRoles"] as List<string> ?? new List<string>(),
                UserPermissions = HttpContext.Items["UserPermissions"] as List<string> ?? new List<string>()
            };

            var permissionTestData = new
            {
                UserContext = userContext,
                RequiredPermission = "Content.Write",
                HasRequiredPermission = true,
                TestMessage = "Content.Write permission test endpoint accessed successfully"
            };

            return Success(permissionTestData, "Content.Write permission authorization test passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in content write permission test endpoint");
            return BadRequest("Content write permission test failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Test endpoint that requires combined Admin role AND Content.Write permission
    /// </summary>
    /// <returns>Combined authorization test result</returns>
    [HttpGet("test-combined-admin-content")]
    [Authorize(Policy = "AdminOrContentManager")] // Requires Admin role AND Content.Write permission
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public IActionResult TestCombinedAdminContent()
    {
        try
        {
            var userContext = new
            {
                UserId = HttpContext.Items["UserId"]?.ToString(),
                UserEmail = HttpContext.Items["UserEmail"]?.ToString(),
                UserRoles = HttpContext.Items["UserRoles"] as List<string> ?? new List<string>(),
                UserPermissions = HttpContext.Items["UserPermissions"] as List<string> ?? new List<string>()
            };

            var combinedTestData = new
            {
                UserContext = userContext,
                RequiredRole = "Admin",
                RequiredPermission = "Content.Write",
                AccessGranted = true,
                TestMessage = "Combined Admin role + Content.Write permission test endpoint accessed successfully"
            };

            return Success(combinedTestData, "Combined authorization test passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in combined authorization test endpoint");
            return BadRequest("Combined authorization test failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Initialize default data (development only)
    /// </summary>
    /// <returns>Initialization result</returns>
    [HttpPost("init-data")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> InitializeDefaultData()
    {
        try
        {
            // This endpoint should only be available in development
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                return BadRequest("This endpoint is only available in development environment");
            }

            var dataInitializationService = HttpContext.RequestServices.GetRequiredService<DataInitializationService>();
            await dataInitializationService.InitializeDefaultDataAsync();

            return Success("Default data initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing default data");
            return BadRequest("Failed to initialize default data: " + ex.Message);
        }
    }

    #endregion
}