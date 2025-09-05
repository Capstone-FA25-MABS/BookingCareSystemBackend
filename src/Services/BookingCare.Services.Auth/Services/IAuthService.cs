using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Auth.Services;

/// <summary>
/// Service interface for Auth service operations
/// </summary>
public interface IAuthService
{
    // Authentication operations
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, Role role);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
    Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    Task<IssueResetTokenResponse> IssueResetTokenAsync(IssueResetTokenRequest request);

    // Account operations
    Task<AccountResponse?> GetAccountByEmailAsync(string email);
    Task<AccountResponse?> GetAccountByPhoneNumberAsync(string phoneNumber);
    Task<Status?> ToggleAccountActiveStatusAsync(Guid id);
    Task<bool> LockAccountAsync(Guid id);
    Task<bool> UnlockAccountAsync(Guid id);

    // Role operations
    Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request);
    Task<RoleResponse?> GetRoleByIdAsync(Guid id);
    Task<RoleResponse?> GetRoleByNameAsync(string name);
    Task<RoleResponse> UpdateRoleAsync(UpdateRoleRequest request);
    Task<bool> DeleteRoleAsync(Guid id);
    Task<RoleListResponse> GetRolesAsync(RoleQueryRequest query);

    // Permission operations
    Task<PermissionResponse> CreatePermissionAsync(CreatePermissionRequest request);
    Task<PermissionResponse?> GetPermissionByIdAsync(Guid id);
    Task<PermissionResponse?> GetPermissionByNameAsync(string name);
    Task<PermissionResponse> UpdatePermissionAsync(UpdatePermissionRequest request);
    Task<bool> DeletePermissionAsync(Guid id);
    Task<PermissionListResponse> GetPermissionsAsync(PermissionQueryRequest query);

    // Account-Role operations
    Task<AccountRoleResponse> AssignRoleToAccountAsync(AssignRoleRequest request);
    Task<bool> RemoveRoleFromAccountAsync(RemoveRoleRequest request);
    Task<List<RoleResponse>> GetAccountRolesAsync(Guid accountId);
    Task<List<AccountResponse>> GetAccountsByRoleAsync(Guid roleId);
    Task<bool> AccountHasRoleAsync(Guid accountId, Guid roleId);
    Task<bool> AccountHasRoleAsync(Guid accountId, string roleName);

    // Role-Permission operations
    Task<RolePermissionResponse> AssignPermissionToRoleAsync(AssignPermissionRequest request);
    Task<bool> RemovePermissionFromRoleAsync(RemovePermissionRequest request);
    Task<List<PermissionResponse>> GetRolePermissionsAsync(Guid roleId);
    Task<List<RoleResponse>> GetRolesByPermissionAsync(Guid permissionId);
    Task<bool> RoleHasPermissionAsync(Guid roleId, Guid permissionId);
    Task<bool> RoleHasPermissionAsync(Guid roleId, string permissionName);

    // Authorization operations
    Task<bool> IsAuthorizedAsync(Guid accountId, string permissionName);
    Task<bool> IsAuthorizedAsync(Guid accountId, List<string> permissionNames);
    Task<List<string>> GetAccountPermissionsAsync(Guid accountId);
}
