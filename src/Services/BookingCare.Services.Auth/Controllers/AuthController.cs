using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Services;
using BookingCare.Shared.Common.Controllers;
using Microsoft.AspNetCore.Authorization;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Auth.Utils;
using System.ComponentModel.DataAnnotations;
using BookingCare.Services.Auth.Constants;
using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Models;
using BookingCare.Shared.Saga.SagaDefinition;

namespace BookingCare.Services.Auth.Controllers;

/// <summary>
/// Authentication controller - handles user authentication and authorization
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly CookieService _cookieService;
    private readonly ISagaManager _sagaManager;
    private readonly ISagaStateStore _sagaStateStore;

    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        CookieService cookieService,
        ISagaManager sagaManager,
        ISagaStateStore sagaStateStore,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _cookieService = cookieService;
        _sagaManager = sagaManager;
        _logger = logger;
        _sagaStateStore = sagaStateStore;
    }

    #region Authentication Operations

    /// <summary>
    /// Health check endpoint - Available in all versions
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "Auth",
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Authenticate account and generate JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response message</returns>
    [HttpPost("login")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validation = ValidateRequest(request);
        if (validation != null) return validation;

        var result = await _authService.LoginAsync(request);
        return Success(result, "Login successful");
    }

    /// <summary>
    /// Register new doctor account
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Authentication response message</returns>
    [HttpPost("register/doctor")]
    [Authorize(Policy = "Role:Clinic")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RegisterDoctor([FromBody] RegisterRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        // Role-specific validation
        var roleValidation = ValidateRoleSpecificRequirements(request, Role.DOCTOR);
        if (roleValidation != null) return roleValidation;

        var result = await _authService.RegisterAsync(request, Role.DOCTOR);
        return Created(result, "Account registered successfully");
    }

    /// <summary>
    /// Register new clinic account
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Authentication response message</returns>
    [HttpPost("register/clinic")]
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RegisterClinic([FromBody] RegisterRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        // Role-specific validation
        var roleValidation = ValidateRoleSpecificRequirements(request, Role.CLINIC);
        if (roleValidation != null) return roleValidation;

        var result = await _authService.RegisterAsync(request, Role.CLINIC);
        return Created(result, "Account registered successfully");
    }

    /// <summary>
    /// Refresh JWT token using refresh token
    /// </summary>
    /// <returns>New authentication response</returns>
    [HttpPost("refresh-token")]
    [MapToApiVersion(ApiVersions.V1_0)]
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
    /// <returns>Success response</returns>
    [HttpPost("logout")]
    [Authorize]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = _cookieService.GetRefreshTokenFromCookies();
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest("Refresh token is missing in cookie");
        }
        await _authService.LogoutAsync(refreshToken);
        return Success("Logout successful");
    }

    /// <summary>
    /// Change account password
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <returns>Success response</returns>
    [HttpPost("change-password")]
    [Authorize]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        await _authService.ChangePasswordAsync(request);
        return Success("Password changed successfully");
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    /// <param name="request">Password reset request</param>
    /// <returns>Success response</returns>
    [HttpPost("forgot-password")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var validation = ValidateRequest(request);
        if (validation != null) return validation;

        await _authService.ForgotPasswordAsync(request);

        // Do not reveal whether the target exists; return generic success
        var response = new
        {
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
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        await _authService.ResetPasswordAsync(request);
        return Success("Password reset successfully");
    }

    /// <summary>
    /// Reset token after OTP verification (phone flow)
    /// </summary>
    /// <param name="request">Reset token request</param>
    /// <returns>Reset token and URL</returns>
    [HttpPost("reset-token")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ResetToken([FromBody] ResetTokenRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        var result = await _authService.ResetTokenAsync(request);
        return Success(result, "Reset token if the phone is registered");
    }

    /// <summary>
    /// Authenticate with Google OAuth2
    /// </summary>
    /// <param name="request">Google login request</param>
    /// <returns>Authentication response</returns>
    [HttpPost("google-login")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GoogleLogin([FromBody] ExternalAuthRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        var result = await _authService.GoogleLoginAsync(request);
        return Success(result, "Google login successful");
    }

    /// <summary>
    /// Authenticate with Facebook OAuth2
    /// </summary>
    /// <param name="request">Facebook login request</param>
    /// <returns>Authentication response</returns>
    [HttpPost("facebook-login")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> FacebookLogin([FromBody] ExternalAuthRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        var result = await _authService.FacebookLoginAsync(request);
        return Success(result, "Facebook login successful");
    }

    /// <summary>
    /// Register new patient using Saga pattern
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Saga execution result</returns>
    [HttpPost("register/patient")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RegisterPatient([FromBody] RegisterRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        // Role-specific validation
        var roleValidation = ValidateRoleSpecificRequirements(request, Role.PATIENT);
        if (roleValidation != null) return roleValidation;

        // Create saga context
        var sagaContext = new SagaContext
        {
            SagaName = "UserRegistration",
            CorrelationId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        sagaContext.SetData("Role", Role.PATIENT.ToString());
        sagaContext.SetData("Email", request.Email);
        sagaContext.SetData("Password", request.Password);
        sagaContext.SetData("FullName", request.FullName);
        sagaContext.SetData("PhoneNumber", request.PhoneNumber);
        sagaContext.SetData("Gender", request.Gender?.ToString());
        sagaContext.SetData("Birthday", request.Birthday?.ToString("yyyy-MM-dd"));
        sagaContext.SetData("Address", request.Address);

        // OTP verification fields for Patient registration
        sagaContext.SetData("Purpose", request.Purpose.ToKey());
        sagaContext.SetData("Channel", string.IsNullOrWhiteSpace(request.Channel) ? "phone" : request.Channel.ToLowerInvariant());
        sagaContext.SetData("Proof", request.Proof);
        sagaContext.SetData("IssuedAt", request.IssuedAt);

        try
        {
            // Execute User Registration Saga synchronously
            var result = await _sagaManager.ExecuteSagaAsync<UserRegistrationSaga>(sagaContext);

            if (result.Status == SagaStatus.Completed)
            {
                return Success("User registration completed successfully");
            }
            else
            {
                return BadRequest(new
                {
                    SagaId = result.SagaId,
                    Status = result.Status.ToString(),
                    Message = "User registration failed",
                    Error = result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing user registration saga");
            return BadRequest(new
            {
                Message = "User registration failed",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Register new doctor using Saga pattern
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Saga execution result</returns>
    [HttpPost("register/doctor-saga")]
    [Authorize(Policy = "Role:Clinic")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RegisterDoctorSaga([FromBody] RegisterRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        // Role-specific validation
        var roleValidation = ValidateRoleSpecificRequirements(request, Role.DOCTOR);
        if (roleValidation != null) return roleValidation;

        // Create saga context
        var sagaContext = new SagaContext
        {
            SagaName = "DoctorRegistration",
            CorrelationId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        sagaContext.SetData("Role", Role.DOCTOR.ToString());
        sagaContext.SetData("Email", request.Email);
        sagaContext.SetData("Password", request.Password);
        sagaContext.SetData("FullName", request.FullName);
        sagaContext.SetData("PhoneNumber", request.PhoneNumber);
        sagaContext.SetData("Gender", request.Gender?.ToString());
        sagaContext.SetData("Address", request.Address);

        // Doctor-specific data from DoctorProfile
        if (request.DoctorProfile != null)
        {
            sagaContext.SetData("Bio", request.DoctorProfile.Bio);
            sagaContext.SetData("YearsOfExperience", request.DoctorProfile.YearsOfExperience);
            sagaContext.SetData("SpecialtyId", request.DoctorProfile.SpecialtyId.ToString());
            sagaContext.SetData("PositionId", request.DoctorProfile.PositionId.ToString());
            sagaContext.SetData("ClinicId", request.DoctorProfile.ClinicId.ToString());
        }

        try
        {
            // Execute Doctor Registration Saga synchronously
            var result = await _sagaManager.ExecuteSagaAsync<DoctorRegistrationSaga>(sagaContext);

            if (result.Status == SagaStatus.Completed)
            {
                return Created(new
                {
                    SagaId = result.SagaId,
                    Status = result.Status.ToString(),
                    Message = "Doctor registration completed successfully",
                }, "Doctor registration completed successfully");
            }
            else
            {
                return BadRequest(new
                {
                    SagaId = result.SagaId,
                    Status = result.Status.ToString(),
                    Message = "Doctor registration failed",
                    Error = result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing doctor registration saga");
            return BadRequest(new
            {
                Message = "Doctor registration failed",
                Error = ex.Message
            });
        }
    }

    #endregion

    #region Account Operations   

    /// <summary>
    /// Ban/Unban account (toggle ACTIVE/INACTIVE)
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Success response</returns>
    [HttpPatch("accounts/{id}/ban-unban")]
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> BanUnban(Guid id)
    {
        var status = await _authService.ToggleAccountActiveStatusAsync(id);
        var message = status == Status.ACTIVE ? "Account unbanned (activated) successfully" : "Account banned (deactivated) successfully";
        return Success(message);
    }

    /// <summary>
    /// Lock account
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Success response</returns>
    [HttpPatch("accounts/{id}/lock")]
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [HttpPatch("accounts/{id}/unlock")]
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

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
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        if (id != request.Id)
        {
            return BadRequest("ID mismatch between route and request body");
        }

        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

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
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        var result = await _authService.CreatePermissionAsync(request);
        return Created(result, "Permission created successfully");
    }

    /// <summary>
    /// Get permission by ID
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <returns>Permission information</returns>
    [HttpGet("permissions/{id}")]
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdatePermission(Guid id, [FromBody] UpdatePermissionRequest request)
    {
        if (id != request.Id)
        {
            return BadRequest("ID mismatch between route and request body");
        }

        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        var result = await _authService.UpdatePermissionAsync(request);
        return Success(result, "Permission updated successfully");
    }

    /// <summary>
    /// Delete permission
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <returns>Success response</returns>
    [HttpDelete("permissions/{id}")]
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> AssignRoleToAccount([FromBody] AssignRoleRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        var result = await _authService.AssignRoleToAccountAsync(request);
        return Success(result, "Role assigned to account successfully");
    }

    /// <summary>
    /// Remove role from account
    /// </summary>
    /// <param name="request">Role removal request</param>
    /// <returns>Success response</returns>
    [HttpDelete("accounts/remove-role")]
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RemoveRoleFromAccount([FromBody] RemoveRoleRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        await _authService.RemoveRoleFromAccountAsync(request);
        return Success("Role removed from account successfully");
    }

    /// <summary>
    /// Get account roles
    /// </summary>
    /// <param name="accountId">Account ID</param>
    /// <returns>List of roles assigned to account</returns>
    [HttpGet("accounts/{accountId}/roles")]
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> AssignPermissionToRole([FromBody] AssignPermissionRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        var result = await _authService.AssignPermissionToRoleAsync(request);
        return Success(result, "Permission assigned to role successfully");
    }

    /// <summary>
    /// Remove permission from role
    /// </summary>
    /// <param name="request">Permission removal request</param>
    /// <returns>Success response</returns>
    [HttpDelete("roles/remove-permission")]
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RemovePermissionFromRole([FromBody] RemovePermissionRequest request)
    {
        var validation = ValidateBasicRequest();
        if (validation != null) return validation;

        await _authService.RemovePermissionFromRoleAsync(request);
        return Success("Permission removed from role successfully");
    }

    /// <summary>
    /// Get role permissions
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <returns>List of permissions assigned to role</returns>
    [HttpGet("roles/{roleId}/permissions")]
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
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
    [Authorize(Policy = "Role:Admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRolesByPermission(Guid permissionId)
    {
        var result = await _authService.GetRolesByPermissionAsync(permissionId);
        return Success(result, "Roles by permission retrieved successfully");
    }

    #endregion


    #region Private Helper Methods

    /// <summary>
    /// Validate role-specific requirements for RegisterRequest
    /// </summary>
    private IActionResult? ValidateRoleSpecificRequirements(RegisterRequest request, Role role)
    {
        var roleValidationResults = request.ValidateByRole(role).ToList();
        if (roleValidationResults.Any())
        {
            return BadRequest(AuthConstants.InvalidRequestData, roleValidationResults.Select(vr => vr.ErrorMessage ?? AuthConstants.ValidationError).ToList());
        }
        return null; // No validation errors
    }

    /// <summary>
    /// Validate custom business rules for IValidatableObject DTOs
    /// </summary>
    private IActionResult? ValidateCustomBusinessRules<T>(T request) where T : IValidatableObject
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);
        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            return BadRequest(AuthConstants.InvalidRequestData, validationResults.Select(vr => vr.ErrorMessage ?? AuthConstants.ValidationError).ToList());
        }
        return null; // No validation errors
    }

    /// <summary>
    /// Handle standard validation flow for requests
    /// </summary>
    private IActionResult? ValidateRequest<T>(T request) where T : IValidatableObject
    {
        // 1. Data Annotations validation (tự động)
        if (!ModelState.IsValid)
        {
            return BadRequest(AuthConstants.InvalidRequestData, ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage ?? AuthConstants.ValidationError)
                .ToList());
        }

        // 2. Custom business rules validation
        return ValidateCustomBusinessRules(request);
    }

    /// <summary>
    /// Handle basic validation flow for simple DTOs (only Data Annotations)
    /// </summary>
    private IActionResult? ValidateBasicRequest()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(AuthConstants.InvalidRequestData, ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage ?? AuthConstants.ValidationError)
                .ToList());
        }
        return null; // No validation errors
    }

    #endregion


}