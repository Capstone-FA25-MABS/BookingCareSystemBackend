using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Models.Entities;

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
    Task<bool> DeleteAccountAsync(Guid id);
    Task<bool> AccountExistsAsync(Guid id);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
    Task<bool> PhoneNumberExistsAsync(string phoneNumber, Guid? excludeId = null);
    Task<(List<AccountEntity> Accounts, int TotalCount)> GetAccountsAsync(AccountQueryRequest query);

    // Role operations
    Task<RoleEntity?> GetRoleByIdAsync(Guid id);
    Task<RoleEntity?> GetRoleByNameAsync(string name);
    Task<RoleEntity> CreateRoleAsync(RoleEntity role);
    Task<RoleEntity> UpdateRoleAsync(RoleEntity role);
    Task<bool> DeleteRoleAsync(Guid id);
    Task<bool> RoleExistsAsync(Guid id);
    Task<bool> RoleNameExistsAsync(string name, Guid? excludeId = null);
    Task<(List<RoleEntity> Roles, int TotalCount)> GetRolesAsync(RoleQueryRequest query);

    // Permission operations
    Task<PermissionEntity?> GetPermissionByIdAsync(Guid id);
    Task<PermissionEntity?> GetPermissionByNameAsync(string name);
    Task<PermissionEntity> CreatePermissionAsync(PermissionEntity permission);
    Task<PermissionEntity> UpdatePermissionAsync(PermissionEntity permission);
    Task<bool> DeletePermissionAsync(Guid id);
    Task<bool> PermissionExistsAsync(Guid id);
    Task<bool> PermissionNameExistsAsync(string name, Guid? excludeId = null);
    Task<(List<PermissionEntity> Permissions, int TotalCount)> GetPermissionsAsync(PermissionQueryRequest query);

    // Account-Role operations
    Task<AccountRoleEntity?> GetAccountRoleAsync(Guid accountId, Guid roleId);
    Task<AccountRoleEntity> AssignRoleToAccountAsync(Guid accountId, Guid roleId);
    Task<bool> RemoveRoleFromAccountAsync(Guid accountId, Guid roleId);
    Task<List<RoleEntity>> GetAccountRolesAsync(Guid accountId);
    Task<List<AccountEntity>> GetAccountsByRoleAsync(Guid roleId);
    Task<bool> AccountHasRoleAsync(Guid accountId, Guid roleId);
    Task<bool> AccountHasRoleAsync(Guid accountId, string roleName);

    // Role-Permission operations
    Task<RolePermissionEntity?> GetRolePermissionAsync(Guid roleId, Guid permissionId);
    Task<RolePermissionEntity> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
    Task<List<PermissionEntity>> GetRolePermissionsAsync(Guid roleId);
    Task<List<RoleEntity>> GetRolesByPermissionAsync(Guid permissionId);
    Task<bool> RoleHasPermissionAsync(Guid roleId, Guid permissionId);
    Task<bool> RoleHasPermissionAsync(Guid roleId, string permissionName);

    // Authentication operations
    Task<bool> ValidateCredentialsAsync(string email, string password);
    Task<bool> ValidateCredentialsWithLockoutAsync(string email, string password);
    Task<bool> IsAccountLockedOutAsync(Guid accountId);
    Task<bool> HasExternalLoginAsync(Guid accountId);
    Task<bool> ChangePasswordAsync(Guid accountId, string newPassword);
    Task<bool> ResetPasswordAsync(string email, string newPassword);
    Task<bool> ResetPasswordWithTokenAsync(string email, string resetToken, string newPassword);
    Task<bool> LockAccountAsync(Guid accountId);
    Task<bool> UnlockAccountAsync(Guid accountId);

    // Password reset token
    Task<(bool Found, Guid AccountId, string Token)> GeneratePasswordResetTokenAsync(string email);
    Task<(bool Found, string Email, Guid AccountId, string Token)> GeneratePasswordResetTokenByPhoneAsync(string phoneNumber);

    // Password policy validation via Identity
    Task<(bool IsValid, IEnumerable<string> Errors)> ValidatePasswordAsync(AccountEntity account, string newPassword);
}
