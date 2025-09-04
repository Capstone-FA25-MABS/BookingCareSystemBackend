using BookingCare.Services.Auth.Exceptions;
using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Models.Entities;
using BookingCare.Services.Auth.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.Auth.Repositories;

/// <summary>
/// Repository implementation for Auth service operations using ASP.NET Core Identity
/// </summary>
public class AuthRepository : IAuthRepository
{
    private readonly UserManager<AccountEntity> _userManager;
    private readonly RoleManager<RoleEntity> _roleManager;
    private readonly AuthDbContext _context;
    private readonly ILogger<AuthRepository> _logger;

    public AuthRepository(
        UserManager<AccountEntity> userManager,
        RoleManager<RoleEntity> roleManager,
        AuthDbContext context,
        ILogger<AuthRepository> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
    }

    #region Account Operations

    /// <summary>
    /// Get account by ID
    /// </summary>
    public async Task<AccountEntity?> GetAccountByIdAsync(Guid id)
    {
        try
        {
            return await _userManager.FindByIdAsync(id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by ID: {AccountId}", id);
            throw new AuthException("Failed to get account by ID", innerException: ex);
        }
    }

    /// <summary>
    /// Get account by email
    /// </summary>
    public async Task<AccountEntity?> GetAccountByEmailAsync(string email)
    {
        try
        {
            return await _userManager.FindByEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by email: {Email}", email);
            throw new AuthException("Failed to get account by email", innerException: ex);
        }
    }

    /// <summary>
    /// Get account by phone number
    /// </summary>
    public async Task<AccountEntity?> GetAccountByPhoneNumberAsync(string phoneNumber)
    {
        try
        {
            return await _userManager.Users
                .FirstOrDefaultAsync(a => a.PhoneNumber == phoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by phone number: {PhoneNumber}", phoneNumber);
            throw new AuthException("Failed to get account by phone number", innerException: ex);
        }
    }

    /// <summary>
    /// Create new account
    /// </summary>
    public async Task<AccountEntity> CreateAccountAsync(AccountEntity account, string password)
    {
        try
        {
            var result = await _userManager.CreateAsync(account, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new AccountValidationException($"Failed to create account: {errors}");
            }

            return account;
        }
        catch (AccountValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account for email: {Email}", account.Email);
            throw new AuthException("Failed to create account", innerException: ex);
        }
    }

    /// <summary>
    /// Update account
    /// </summary>
    public async Task<AccountEntity> UpdateAccountAsync(AccountEntity account)
    {
        try
        {
            var result = await _userManager.UpdateAsync(account);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new AccountValidationException($"Failed to update account: {errors}");
            }

            return account;
        }
        catch (AccountValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account: {AccountId}", account.Id);
            throw new AuthException("Failed to update account", innerException: ex);
        }
    }

    /// <summary>
    /// Delete account
    /// </summary>
    public async Task<bool> DeleteAccountAsync(Guid id)
    {
        try
        {
            var account = await GetAccountByIdAsync(id);
            if (account == null)
                return false;

            var result = await _userManager.DeleteAsync(account);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account: {AccountId}", id);
            throw new AuthException("Failed to delete account", innerException: ex);
        }
    }

    /// <summary>
    /// Check if account exists
    /// </summary>
    public async Task<bool> AccountExistsAsync(Guid id)
    {
        try
        {
            return await _userManager.FindByIdAsync(id.ToString()) != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking account existence: {AccountId}", id);
            throw new AuthException("Failed to check account existence", innerException: ex);
        }
    }

    /// <summary>
    /// Check if email exists
    /// </summary>
    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null)
    {
        try
        {
            var account = await _userManager.FindByEmailAsync(email);
            if (account == null) return false;
            
            if (excludeId.HasValue && account.Id == excludeId.Value)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email existence: {Email}", email);
            throw new AuthException("Failed to check email existence", innerException: ex);
        }
    }

    /// <summary>
    /// Check if phone number exists
    /// </summary>
    public async Task<bool> PhoneNumberExistsAsync(string phoneNumber, Guid? excludeId = null)
    {
        try
        {
            var account = await _userManager.Users
                .FirstOrDefaultAsync(a => a.PhoneNumber == phoneNumber);
            
            if (account == null) return false;
            
            if (excludeId.HasValue && account.Id == excludeId.Value)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking phone number existence: {PhoneNumber}", phoneNumber);
            throw new AuthException("Failed to check phone number existence", innerException: ex);
        }
    }

    /// <summary>
    /// Get accounts with filtering and pagination
    /// </summary>
    public async Task<(List<AccountEntity> Accounts, int TotalCount)> GetAccountsAsync(AccountQueryRequest query)
    {
        try
        {
            var queryable = _userManager.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(query.SearchTerm))
                queryable = queryable.Where(a => a.Email.Contains(query.SearchTerm) || a.UserName.Contains(query.SearchTerm));

            //if (!string.IsNullOrEmpty(query.Status))
            //    queryable = queryable.Where(a => a.Status == query.Status);

            var totalCount = await queryable.CountAsync();

            // Apply pagination
            var accounts = await queryable
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (accounts, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts with query");
            throw new AuthException("Failed to get accounts", innerException: ex);
        }
    }

    #endregion

    #region Role Operations

    /// <summary>
    /// Get role by ID
    /// </summary>
    public async Task<RoleEntity?> GetRoleByIdAsync(Guid id)
    {
        try
        {
            return await _roleManager.FindByIdAsync(id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role by ID: {RoleId}", id);
            throw new AuthException("Failed to get role by ID", innerException: ex);
        }
    }

    /// <summary>
    /// Get role by name
    /// </summary>
    public async Task<RoleEntity?> GetRoleByNameAsync(string name)
    {
        try
        {
            return await _roleManager.FindByNameAsync(name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role by name: {RoleName}", name);
            throw new AuthException("Failed to get role by name", innerException: ex);
        }
    }

    /// <summary>
    /// Create new role
    /// </summary>
    public async Task<RoleEntity> CreateRoleAsync(RoleEntity role)
    {
        try
        {
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new RoleValidationException($"Failed to create role: {errors}");
            }

            return role;
        }
        catch (RoleValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role: {RoleName}", role.Name);
            throw new AuthException("Failed to create role", innerException: ex);
        }
    }

    /// <summary>
    /// Update role
    /// </summary>
    public async Task<RoleEntity> UpdateRoleAsync(RoleEntity role)
    {
        try
        {
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new RoleValidationException($"Failed to update role: {errors}");
            }

            return role;
        }
        catch (RoleValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role: {RoleId}", role.Id);
            throw new AuthException("Failed to update role", innerException: ex);
        }
    }

    /// <summary>
    /// Delete role
    /// </summary>
    public async Task<bool> DeleteRoleAsync(Guid id)
    {
        try
        {
            var role = await GetRoleByIdAsync(id);
            if (role == null)
                return false;

            var result = await _roleManager.DeleteAsync(role);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role: {RoleId}", id);
            throw new AuthException("Failed to delete role", innerException: ex);
        }
    }

    /// <summary>
    /// Check if role exists
    /// </summary>
    public async Task<bool> RoleExistsAsync(Guid id)
    {
        try
        {
            return await _roleManager.FindByIdAsync(id.ToString()) != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking role existence: {RoleId}", id);
            throw new AuthException("Failed to check role existence", innerException: ex);
        }
    }

    /// <summary>
    /// Check if role name exists
    /// </summary>
    public async Task<bool> RoleNameExistsAsync(string name, Guid? excludeId = null)
    {
        try
        {
            var role = await _roleManager.FindByNameAsync(name);
            if (role == null) return false;
            
            if (excludeId.HasValue && role.Id == excludeId.Value)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking role name existence: {RoleName}", name);
            throw new AuthException("Failed to check role name existence", innerException: ex);
        }
    }

    /// <summary>
    /// Get roles with filtering and pagination
    /// </summary>
    public async Task<(List<RoleEntity> Roles, int TotalCount)> GetRolesAsync(RoleQueryRequest query)
    {
        try
        {
            var queryable = _roleManager.Roles.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(query.SearchTerm))
                queryable = queryable.Where(r => r.Name.Contains(query.SearchTerm));

            var totalCount = await queryable.CountAsync();

            // Apply pagination
            var roles = await queryable
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (roles, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles with query");
            throw new AuthException("Failed to get roles", innerException: ex);
        }
    }

    #endregion

    #region Permission Operations

    /// <summary>
    /// Get permission by ID
    /// </summary>
    public async Task<PermissionEntity?> GetPermissionByIdAsync(Guid id)
    {
        try
        {
            return await _context.Permissions.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission by ID: {PermissionId}", id);
            throw new AuthException("Failed to get permission by ID", innerException: ex);
        }
    }

    /// <summary>
    /// Get permission by name
    /// </summary>
    public async Task<PermissionEntity?> GetPermissionByNameAsync(string name)
    {
        try
        {
            return await _context.Permissions.FirstOrDefaultAsync(p => p.Name == name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission by name: {PermissionName}", name);
            throw new AuthException("Failed to get permission by name", innerException: ex);
        }
    }

    /// <summary>
    /// Create new permission
    /// </summary>
    public async Task<PermissionEntity> CreatePermissionAsync(PermissionEntity permission)
    {
        try
        {
            var result = await _context.Permissions.AddAsync(permission);
            await _context.SaveChangesAsync();
            return result.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission: {PermissionName}", permission.Name);
            throw new AuthException("Failed to create permission", innerException: ex);
        }
    }

    /// <summary>
    /// Update permission
    /// </summary>
    public async Task<PermissionEntity> UpdatePermissionAsync(PermissionEntity permission)
    {
        try
        {
            _context.Permissions.Update(permission);
            await _context.SaveChangesAsync();
            return permission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission: {PermissionId}", permission.Id);
            throw new AuthException("Failed to update permission", innerException: ex);
        }
    }

    /// <summary>
    /// Delete permission
    /// </summary>
    public async Task<bool> DeletePermissionAsync(Guid id)
    {
        try
        {
            var permission = await GetPermissionByIdAsync(id);
            if (permission == null)
                return false;

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission: {PermissionId}", id);
            throw new AuthException("Failed to delete permission", innerException: ex);
        }
    }

    /// <summary>
    /// Check if permission exists
    /// </summary>
    public async Task<bool> PermissionExistsAsync(Guid id)
    {
        try
        {
            return await _context.Permissions.AnyAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission existence: {PermissionId}", id);
            throw new AuthException("Failed to check permission existence", innerException: ex);
        }
    }

    /// <summary>
    /// Check if permission name exists
    /// </summary>
    public async Task<bool> PermissionNameExistsAsync(string name, Guid? excludeId = null)
    {
        try
        {
            var query = _context.Permissions.AsQueryable();
            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);

            return await query.AnyAsync(p => p.Name == name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission name existence: {PermissionName}", name);
            throw new AuthException("Failed to check permission name existence", innerException: ex);
        }
    }

    /// <summary>
    /// Get permissions with filtering and pagination
    /// </summary>
    public async Task<(List<PermissionEntity> Permissions, int TotalCount)> GetPermissionsAsync(PermissionQueryRequest query)
    {
        try
        {
            var queryable = _context.Permissions.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(query.SearchTerm))
                queryable = queryable.Where(p => p.Name.Contains(query.SearchTerm));

            var totalCount = await queryable.CountAsync();

            // Apply pagination
            var permissions = await queryable
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (permissions, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions with query");
            throw new AuthException("Failed to get permissions", innerException: ex);
        }
    }

    #endregion

    #region Account-Role Operations

    /// <summary>
    /// Get account-role relationship
    /// </summary>
    public async Task<AccountRoleEntity?> GetAccountRoleAsync(Guid accountId, Guid roleId)
    {
        try
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == accountId && ur.RoleId == roleId);
            
            if (userRole == null)
                return null;
                
            return new AccountRoleEntity
            {
                UserId = userRole.UserId,
                RoleId = userRole.RoleId,
                CreatedAt = DateTime.UtcNow, // Note: Identity doesn't store these timestamps
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account-role relationship: AccountId={AccountId}, RoleId={RoleId}", accountId, roleId);
            throw new AuthException("Failed to get account-role relationship", innerException: ex);
        }
    }

    /// <summary>
    /// Assign role to account
    /// </summary>
    public async Task<AccountRoleEntity> AssignRoleToAccountAsync(Guid accountId, Guid roleId)
    {
        try
        {
            var result = await _userManager.AddToRoleAsync(
                await GetAccountByIdAsync(accountId) ?? throw new AccountNotFoundException(accountId),
                (await GetRoleByIdAsync(roleId))?.Name ?? throw new RoleNotFoundException(roleId));

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new AuthException($"Failed to assign role to account: {errors}");
            }

            return new AccountRoleEntity
            {
                UserId = accountId,
                RoleId = roleId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (AccountNotFoundException)
        {
            throw;
        }
        catch (RoleNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to account: AccountId={AccountId}, RoleId={RoleId}", accountId, roleId);
            throw new AuthException("Failed to assign role to account", innerException: ex);
        }
    }

    /// <summary>
    /// Remove role from account
    /// </summary>
    public async Task<bool> RemoveRoleFromAccountAsync(Guid accountId, Guid roleId)
    {
        try
        {
            var account = await GetAccountByIdAsync(accountId);
            var role = await GetRoleByIdAsync(roleId);

            if (account == null || role == null)
                return false;

            var result = await _userManager.RemoveFromRoleAsync(account, role.Name);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from account: AccountId={AccountId}, RoleId={RoleId}", accountId, roleId);
            throw new AuthException("Failed to remove role from account", innerException: ex);
        }
    }

    /// <summary>
    /// Get account roles
    /// </summary>
    public async Task<List<RoleEntity>> GetAccountRolesAsync(Guid accountId)
    {
        try
        {
            var account = await GetAccountByIdAsync(accountId);
            if (account == null)
                return new List<RoleEntity>();

            var roleNames = await _userManager.GetRolesAsync(account);
            var roles = new List<RoleEntity>();

            foreach (var roleName in roleNames)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                    roles.Add(role);
            }

            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account roles: {AccountId}", accountId);
            throw new AuthException("Failed to get account roles", innerException: ex);
        }
    }

    /// <summary>
    /// Get accounts by role
    /// </summary>
    public async Task<List<AccountEntity>> GetAccountsByRoleAsync(Guid roleId)
    {
        try
        {
            var role = await GetRoleByIdAsync(roleId);
            if (role == null)
                return new List<AccountEntity>();

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            return usersInRole.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts by role: {RoleId}", roleId);
            throw new AuthException("Failed to get accounts by role", innerException: ex);
        }
    }

    /// <summary>
    /// Check if account has role
    /// </summary>
    public async Task<bool> AccountHasRoleAsync(Guid accountId, Guid roleId)
    {
        try
        {
            var account = await GetAccountByIdAsync(accountId);
            var role = await GetRoleByIdAsync(roleId);

            if (account == null || role == null)
                return false;

            return await _userManager.IsInRoleAsync(account, role.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if account has role: AccountId={AccountId}, RoleId={RoleId}", accountId, roleId);
            throw new AuthException("Failed to check if account has role", innerException: ex);
        }
    }

    /// <summary>
    /// Check if account has role by name
    /// </summary>
    public async Task<bool> AccountHasRoleAsync(Guid accountId, string roleName)
    {
        try
        {
            var account = await GetAccountByIdAsync(accountId);
            if (account == null)
                return false;

            return await _userManager.IsInRoleAsync(account, roleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if account has role: AccountId={AccountId}, RoleName={RoleName}", accountId, roleName);
            throw new AuthException("Failed to check if account has role", innerException: ex);
        }
    }

    #endregion

    #region Role-Permission Operations

    /// <summary>
    /// Get role-permission relationship
    /// </summary>
    public async Task<RolePermissionEntity?> GetRolePermissionAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            return await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role-permission relationship: RoleId={RoleId}, PermissionId={PermissionId}", roleId, permissionId);
            throw new AuthException("Failed to get role-permission relationship", innerException: ex);
        }
    }

    /// <summary>
    /// Assign permission to role
    /// </summary>
    public async Task<RolePermissionEntity> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            var rolePermission = new RolePermissionEntity
            {
                RoleId = roleId,
                PermissionId = permissionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _context.RolePermissions.AddAsync(rolePermission);
            await _context.SaveChangesAsync();
            return result.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission to role: RoleId={RoleId}, PermissionId={PermissionId}", roleId, permissionId);
            throw new AuthException("Failed to assign permission to role", innerException: ex);
        }
    }

    /// <summary>
    /// Remove permission from role
    /// </summary>
    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            var rolePermission = await GetRolePermissionAsync(roleId, permissionId);
            if (rolePermission == null)
                return false;

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission from role: RoleId={RoleId}, PermissionId={PermissionId}", roleId, permissionId);
            throw new AuthException("Failed to remove permission from role", innerException: ex);
        }
    }

    /// <summary>
    /// Get role permissions
    /// </summary>
    public async Task<List<PermissionEntity>> GetRolePermissionsAsync(Guid roleId)
    {
        try
        {
            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission)
                .ToListAsync();

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role permissions: {RoleId}", roleId);
            throw new AuthException("Failed to get role permissions", innerException: ex);
        }
    }

    /// <summary>
    /// Get roles by permission
    /// </summary>
    public async Task<List<RoleEntity>> GetRolesByPermissionAsync(Guid permissionId)
    {
        try
        {
            var roles = await _context.RolePermissions
                .Where(rp => rp.PermissionId == permissionId)
                .Include(rp => rp.Role)
                .Select(rp => rp.Role)
                .ToListAsync();

            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles by permission: {PermissionId}", permissionId);
            throw new AuthException("Failed to get roles by permission", innerException: ex);
        }
    }

    /// <summary>
    /// Check if role has permission
    /// </summary>
    public async Task<bool> RoleHasPermissionAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            return await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if role has permission: RoleId={RoleId}, PermissionId={PermissionId}", roleId, permissionId);
            throw new AuthException("Failed to check if role has permission", innerException: ex);
        }
    }

    /// <summary>
    /// Check if role has permission by name
    /// </summary>
    public async Task<bool> RoleHasPermissionAsync(Guid roleId, string permissionName)
    {
        try
        {
            var permission = await GetPermissionByNameAsync(permissionName);
            if (permission == null)
                return false;

            return await RoleHasPermissionAsync(roleId, permission.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if role has permission: RoleId={RoleId}, PermissionName={PermissionName}", roleId, permissionName);
            throw new AuthException("Failed to check if role has permission", innerException: ex);
        }
    }

    #endregion

    #region Authentication Operations

    /// <summary>
    /// Validate credentials
    /// </summary>
    public async Task<bool> ValidateCredentialsAsync(string email, string password)
    {
        try
        {
            var account = await _userManager.FindByEmailAsync(email);
            if (account == null)
                return false;

            return await _userManager.CheckPasswordAsync(account, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credentials for email: {Email}", email);
            throw new AuthException("Failed to validate credentials", innerException: ex);
        }
    }

    /// <summary>
    /// Validate credentials with lockout support
    /// </summary>
    public async Task<bool> ValidateCredentialsWithLockoutAsync(string email, string password)
    {
        try
        {
            var account = await _userManager.FindByEmailAsync(email);
            if (account == null)
                return false;

            // Check if account is locked out
            if (await _userManager.IsLockedOutAsync(account))
            {
                _logger.LogWarning("Account is locked out: {Email}", email);
                return false;
            }

            // Check password
            var isValid = await _userManager.CheckPasswordAsync(account, password);
            
            if (isValid)
            {
                // Reset failed access count on successful login
                await _userManager.ResetAccessFailedCountAsync(account);
                _logger.LogInformation("Successful login for account: {Email}", email);
            }
            else
            {
                // Increment failed access count
                await _userManager.AccessFailedAsync(account);
                _logger.LogWarning("Failed login attempt for account: {Email}", email);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credentials with lockout for email: {Email}", email);
            throw new AuthException("Failed to validate credentials", innerException: ex);
        }
    }

    /// <summary>
    /// Check if account is locked out
    /// </summary>
    public async Task<bool> IsAccountLockedOutAsync(Guid accountId)
    {
        try
        {
            var account = await _userManager.FindByIdAsync(accountId.ToString());
            if (account == null)
                return false;

            return await _userManager.IsLockedOutAsync(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking lockout status for account: {AccountId}", accountId);
            throw new AuthException("Failed to check lockout status", innerException: ex);
        }
    }

    /// <summary>
    /// Check if account has external login providers (Google, Facebook, etc.)
    /// </summary>
    public async Task<bool> HasExternalLoginAsync(Guid accountId)
    {
        try
        {
            var account = await _userManager.FindByIdAsync(accountId.ToString());
            if (account == null)
                return false;

            var logins = await _userManager.GetLoginsAsync(account);
            return logins.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking external login status for account: {AccountId}", accountId);
            throw new AuthException("Failed to check external login status", innerException: ex);
        }
    }

    /// <summary>
    /// Change password
    /// </summary>
    public async Task<bool> ChangePasswordAsync(Guid accountId, string newPassword)
    {
        try
        {
            var account = await GetAccountByIdAsync(accountId);
            if (account == null)
                return false;

            var token = await _userManager.GeneratePasswordResetTokenAsync(account);
            var result = await _userManager.ResetPasswordAsync(account, token, newPassword);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for account: {AccountId}", accountId);
            throw new AuthException("Failed to change password", innerException: ex);
        }
    }

    /// <summary>
    /// Reset password
    /// </summary>
    public async Task<bool> ResetPasswordAsync(string email, string newPassword)
    {
        try
        {
            var account = await _userManager.FindByEmailAsync(email);
            if (account == null)
                return false;

            var token = await _userManager.GeneratePasswordResetTokenAsync(account);
            var result = await _userManager.ResetPasswordAsync(account, token, newPassword);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for email: {Email}", email);
            throw new AuthException("Failed to reset password", innerException: ex);
        }
    }

    /// <summary>
    /// Reset password using Identity reset token
    /// </summary>
    public async Task<bool> ResetPasswordWithTokenAsync(string email, string resetToken, string newPassword)
    {
        try
        {
            var account = await _userManager.FindByEmailAsync(email);
            if (account == null)
                return false;

            var result = await _userManager.ResetPasswordAsync(account, resetToken, newPassword);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password with token for email: {Email}", email);
            throw new AuthException("Failed to reset password with token", innerException: ex);
        }
    }

    /// <summary>
    /// Lock account
    /// </summary>
    public async Task<bool> LockAccountAsync(Guid accountId)
    {
        try
        {
            var account = await GetAccountByIdAsync(accountId);
            if (account == null)
                return false;

            var result = await _userManager.SetLockoutEndDateAsync(account, DateTimeOffset.MaxValue);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking account: {AccountId}", accountId);
            throw new AuthException("Failed to lock account", innerException: ex);
        }
    }

    /// <summary>
    /// Unlock account
    /// </summary>
    public async Task<bool> UnlockAccountAsync(Guid accountId)
    {
        try
        {
            var account = await GetAccountByIdAsync(accountId);
            if (account == null)
                return false;

            var result = await _userManager.SetLockoutEndDateAsync(account, null);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking account: {AccountId}", accountId);
            throw new AuthException("Failed to unlock account", innerException: ex);
        }
    }



    #endregion

    #region Password reset token

    /// <summary>
    /// Generate password reset token for an account by email
    /// </summary>
    public async Task<(bool Found, Guid AccountId, string Token)> GeneratePasswordResetTokenAsync(string email)
    {
        try
        {
            var account = await _userManager.FindByEmailAsync(email);
            if (account == null)
            {
                return (false, Guid.Empty, string.Empty);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(account);
            return (true, account.Id, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating password reset token for email: {Email}", email);
            throw new AuthException("Failed to generate password reset token", innerException: ex);
        }
    }

    #endregion

    #region Password reset token by phone

    /// <summary>
    /// Generate password reset token by phone number
    /// </summary>
    public async Task<(bool Found, string Email, Guid AccountId, string Token)> GeneratePasswordResetTokenByPhoneAsync(string phoneNumber)
    {
        try
        {
            var account = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (account == null)
            {
                return (false, string.Empty, Guid.Empty, string.Empty);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(account);
            return (true, account.Email ?? string.Empty, account.Id, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating password reset token for phone: {Phone}", phoneNumber);
            throw new AuthException("Failed to generate password reset token by phone", innerException: ex);
        }
    }

    #endregion

    #region Password policy validation

    public async Task<(bool IsValid, IEnumerable<string> Errors)> ValidatePasswordAsync(AccountEntity account, string newPassword)
    {
        try
        {
            var validator = new PasswordValidator<AccountEntity>();
            var result = await validator.ValidateAsync(_userManager, account, newPassword);
            if (result.Succeeded)
            {
                return (true, Array.Empty<string>());
            }
            return (false, result.Errors.Select(e => e.Description));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password for account: {AccountId}", account.Id);
            throw new AuthException("Failed to validate password", innerException: ex);
        }
    }

    #endregion
}
