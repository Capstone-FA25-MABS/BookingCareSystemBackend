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
using BookingCare.Shared.Common.Enums;

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
    [HttpPost("register/patient")]
    public async Task<IActionResult> RegisterPatient([FromBody] RegisterRequest request)
    {      
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var result = await _authService.RegisterAsync(request, Role.PATIENT);
        return Created(result, "Account registered successfully");             
    }

    /// <summary>
    /// Register new account
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("register/doctor")]
    [Authorize(Policy = "Role:Clinic")]
    public async Task<IActionResult> RegisterDoctor([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var result = await _authService.RegisterAsync(request, Role.DOCTOR);
        return Created(result, "Account registered successfully");
    }

    /// <summary>
    /// Register new account
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("register/clinic")]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> RegisterClinic([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var result = await _authService.RegisterAsync(request, Role.CLINIC);
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
    /// Ban/Unban account (toggle ACTIVE/INACTIVE)
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Success response</returns>
    [HttpPatch("accounts/ban-unban/{id}")]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> BanUnban(Guid id)
    {
        var status = await _authService.ToggleAccountActiveStatusAsync(id);
        if (status == null)
        {
            return NotFound($"Account with ID {id} not found");
        }

        var message = status == Status.ACTIVE ? "Account unbanned (activated) successfully" : "Account banned (deactivated) successfully";
        return Success(message);
    }

    /// <summary>
    /// Lock account
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Success response</returns>
    [HttpPatch("accounts/lock/{id}")]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> LockAccount(Guid id)
    {
        var result = await _authService.LockAccountAsync(id);
        if (!result)
        {
            return NotFound($"Account with ID {id} not found");
        }

        return Success("Account locked successfully");
    }

    /// <summary>
    /// Unlock account
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Success response</returns>
    [HttpPatch("accounts/unlock/{id}")]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> UnlockAccount(Guid id)
    {
        var result = await _authService.UnlockAccountAsync(id);
        if (!result)
        {
            return NotFound($"Account with ID {id} not found");
        }

        return Success("Account unlocked successfully");
    }

    #endregion

    #region Role Operations

    /// <summary>
    /// Create new role
    /// </summary>
    /// <param name="request">Role creation request</param>
    /// <returns>Created role information</returns>
    [HttpPost("roles")]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
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

    /// <summary>
    /// Get role by ID
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <returns>Role information</returns>
    [HttpGet("roles/{id}")]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> GetRole(Guid id)
    {
        var role = await _authService.GetRoleByIdAsync(id);
        if (role == null)
        {
            return NotFound($"Role with ID {id} not found");
        }

        return Success(role, "Role retrieved successfully");
    }

    /// <summary>
    /// Get role by name
    /// </summary>
    /// <param name="name">Role name</param>
    /// <returns>Role information</returns>
    [HttpGet("roles/by-name/{name}")]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> GetRoleByName(string name)
    {
        var role = await _authService.GetRoleByNameAsync(name);
        if (role == null)
        {
            return NotFound($"Role with name '{name}' not found");
        }

        return Success(role, "Role retrieved successfully");
    }

    /// <summary>
    /// Update role information
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="request">Role update request</param>
    /// <returns>Updated role information</returns>
    [HttpPut("roles/{id}")]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
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

    /// <summary>
    /// Delete role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <returns>Success response</returns>
    [HttpDelete("roles/{id}")]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var result = await _authService.DeleteRoleAsync(id);
        if (!result)
        {
            return NotFound($"Role with ID {id} not found");
        }

        return Success("Role deleted successfully");
    }

    /// <summary>
    /// Get roles with filtering and pagination
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <returns>Paginated list of roles</returns>
    [HttpGet("roles")]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> GetRoles([FromQuery] RoleQueryRequest query)
    {
        var result = await _authService.GetRolesAsync(query);
        return Success(result, "Roles retrieved successfully");
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
        var permission = await _authService.GetPermissionByIdAsync(id);
        if (permission == null)
        {
            return NotFound($"Permission with ID {id} not found");
        }

        return Success(permission, "Permission retrieved successfully");
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
        var permission = await _authService.GetPermissionByNameAsync(name);
        if (permission == null)
        {
            return NotFound($"Permission with name '{name}' not found");
        }

        return Success(permission, "Permission retrieved successfully");
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
        var result = await _authService.DeletePermissionAsync(id);
        if (!result)
        {
            return NotFound($"Permission with ID {id} not found");
        }

        return Success("Permission deleted successfully");
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
        var result = await _authService.GetPermissionsAsync(query);
        return Success(result, "Permissions retrieved successfully");
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
        var result = await _authService.GetAccountRolesAsync(accountId);
        return Success(result, "Account roles retrieved successfully");
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
        var result = await _authService.GetAccountsByRoleAsync(roleId);
        return Success(result, "Accounts by role retrieved successfully");
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
        var result = await _authService.GetRolePermissionsAsync(roleId);
        return Success(result, "Role permissions retrieved successfully");
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
        var result = await _authService.GetRolesByPermissionAsync(permissionId);
        return Success(result, "Roles by permission retrieved successfully");
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
        var result = await _authService.IsAuthorizedAsync(accountId, permissionName);
        return Success(result, result ? "Account is authorized" : "Account is not authorized");
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
        var result = await _authService.GetAccountPermissionsAsync(accountId);
        return Success(result, "Account permissions retrieved successfully");
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

    /// <summary>
    /// Initialize default data (development only)
    /// </summary>
    /// <returns>Initialization result</returns>
    [HttpPost("init-data")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> InitializeDefaultData()
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

    #endregion
}