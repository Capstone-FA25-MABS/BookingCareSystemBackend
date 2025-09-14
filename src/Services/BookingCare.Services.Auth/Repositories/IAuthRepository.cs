using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Models.Entities;
using Microsoft.AspNetCore.Identity;
namespace BookingCare.Services.Auth.Repositories;

/// <summary>
/// Repository interface for Auth service operations
/// </summary>
public interface IAuthRepository
{
    // Account operations
    Task<AccountEntity?> GetAccountByIdAsync(Guid id);
    Task<AccountEntity?> GetAccountByEmailAsync(string email);
    Task<AccountEntity?> GetAccountByPhoneNumberAsync(string phoneNumber);
    Task<AccountEntity> CreateAccountAsync(AccountEntity account, string password);
    Task<AccountEntity> UpdateAccountAsync(AccountEntity account);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> PhoneNumberExistsAsync(string phoneNumber);

    // Role operations
    Task<RoleEntity?> GetRoleByIdAsync(Guid id);
    Task<RoleEntity?> GetRoleByNameAsync(string name);
    Task<RoleEntity> CreateRoleAsync(RoleEntity role);
    Task<RoleEntity> UpdateRoleAsync(RoleEntity role);
    Task<bool> DeleteRoleAsync(Guid id);
    Task<bool> RoleNameExistsAsync(string name, Guid? excludeId = null);
    Task<(List<RoleEntity> Roles, int TotalCount)> GetRolesAsync(RoleQueryRequest query);

    // Permission operations
    Task<PermissionEntity?> GetPermissionByIdAsync(Guid id);
    Task<PermissionEntity?> GetPermissionByNameAsync(string name);
    Task<PermissionEntity> CreatePermissionAsync(PermissionEntity permission);
    Task<PermissionEntity> UpdatePermissionAsync(PermissionEntity permission);
    Task<bool> DeletePermissionAsync(Guid id);
    Task<bool> PermissionNameExistsAsync(string name, Guid? excludeId = null);
    Task<(List<PermissionEntity> Permissions, int TotalCount)> GetPermissionsAsync(PermissionQueryRequest query);

    // Account-Role operations
    Task<bool> RoleAlreadyAssignedAsync(AccountEntity account, string roleName);
    Task<AccountRoleEntity> AssignRoleToAccountAsync(AccountEntity account, RoleEntity role);
    Task<bool> RemoveRoleFromAccountAsync(AccountEntity account, RoleEntity role);
    Task<List<RoleEntity>> GetAccountRolesAsync(AccountEntity account);
    Task<List<AccountEntity>> GetAccountsByRoleAsync(RoleEntity role);

    // Account-Permission operations
    Task<List<string>> GetAccountPermissionsAsync(AccountEntity account);

    // Role-Permission operations
    Task<RolePermissionEntity> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
    Task<List<PermissionEntity>> GetRolePermissionsAsync(Guid roleId);
    Task<List<RoleEntity>> GetRolesByPermissionAsync(Guid permissionId);
    Task<bool> RoleHasPermissionAsync(Guid roleId, Guid permissionId);

    // Authentication operations
    Task<bool> ValidateCredentialsAsync(AccountEntity account, string password);
    Task<bool> ValidateCredentialsWithLockoutAsync(AccountEntity account, string password);
    Task<bool> IsAccountLockedOutAsync(AccountEntity account);
    Task<bool> HasExternalLoginAsync(AccountEntity account);
    Task<bool> ChangePasswordAsync(AccountEntity account, string newPassword);
    Task<bool> ResetPasswordWithTokenAsync(AccountEntity account, string resetToken, string newPassword);
    Task<bool> LockAccountAsync(AccountEntity account);
    Task<bool> UnlockAccountAsync(AccountEntity account);

    // Password reset token
    Task<(bool Found, Guid AccountId, string Token)> GeneratePasswordResetTokenAsync(string email);
    Task<(bool Found, string Email, string Token)> GeneratePasswordResetTokenByPhoneAsync(string phoneNumber);

    // Password policy validation via Identity
    Task<(bool IsValid, IEnumerable<string> Errors)> ValidatePasswordAsync(AccountEntity account, string newPassword);

    // Refresh Token operations
    Task<RefreshTokenEntity> CreateRefreshTokenAsync(Guid accountId);
    Task<(bool IsValid, AccountEntity? Account, RefreshTokenEntity? Token)> ValidateRefreshTokenAsync(string token);
    Task<bool> DeleteRefreshTokenAsync(string token);

    // External Login operations
    Task<IdentityResult> CreateAccountAsync(AccountEntity user);
    Task<bool> HasExternalLoginAsync(Guid userId, string loginProvider, string providerKey);
    Task AddExternalLoginAsync(Guid userId, string loginProvider, string providerKey);
}
