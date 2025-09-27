using BookingCare.Services.Auth.Exceptions;
using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Models.Entities;
using BookingCare.Services.Auth.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
    private readonly IConfiguration _configuration;

    public AuthRepository(
        UserManager<AccountEntity> userManager,
        RoleManager<RoleEntity> roleManager,
        AuthDbContext context,
        ILogger<AuthRepository> logger,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
        _configuration = configuration;
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
    public async Task<bool> DeleteAccountAsync(AccountEntity account)
    {
        try
        {
            var result = await _userManager.DeleteAsync(account);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to delete account {AccountId}: {Errors}", account.Id, errors);
                return false;
            }

            _logger.LogInformation("Account deleted successfully: {AccountId}", account.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account: {AccountId}", account.Id);
            return false;
        }
    }

    /// <summary>
    /// Check if email exists using AnyAsync for optimal performance
    /// </summary>
    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            return await _userManager.Users
                .AnyAsync(u => u.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email existence: {Email}", email);
            throw new AuthException("Failed to check email existence", innerException: ex);
        }
    }

    /// <summary>
    /// Check if phone number exists using AnyAsync for optimal performance
    /// </summary>
    public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
    {
        try
        {
            return await _userManager.Users
                .AnyAsync(u => u.PhoneNumber == phoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking phone number existence: {PhoneNumber}", phoneNumber);
            throw new AuthException("Failed to check phone number existence", innerException: ex);
        }
    }

    /// <summary>
    /// Get accounts by list of IDs with their status
    /// </summary>
    public async Task<List<AccountEntity>> GetAccountsByIdsAsync(List<Guid> accountIds)
    {
        try
        {
            if (accountIds == null || accountIds.Count == 0)
            {
                return new List<AccountEntity>();
            }

            return await _userManager.Users
                .Where(u => accountIds.Contains(u.Id))
                .Select(u => new AccountEntity
                {
                    Id = u.Id,
                    Status = u.Status
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts by IDs: {AccountIds}", string.Join(", ", accountIds));
            throw new AuthException("Failed to get accounts by IDs", innerException: ex);
        }
    }

    /// <summary>
    /// Get accounts with their roles by list of IDs
    /// </summary>
    public async Task<List<(Guid AccountId, List<string> Roles)>> GetAccountsWithRolesAsync(List<Guid> accountIds)
    {
        try
        {
            var accounts = await _userManager.Users
                .Where(u => accountIds.Contains(u.Id))
                .ToListAsync();

            var result = new List<(Guid AccountId, List<string> Roles)>();

            foreach (var account in accounts)
            {
                var roles = await _userManager.GetRolesAsync(account);
                result.Add((account.Id, roles.ToList()));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts with roles by IDs: {AccountIds}", string.Join(", ", accountIds));
            throw new AuthException("Failed to retrieve accounts with roles by IDs", innerException: ex);
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
    /// Check if role name exists using AnyAsync for optimal performance (case-insensitive)
    /// </summary>
    public async Task<bool> RoleNameExistsAsync(string name, Guid? excludeId = null)
    {
        try
        {
            if (excludeId.HasValue)
            {
                return await _roleManager.Roles
                    .AnyAsync(r => r.Name!.ToLower() == name.ToLower() && r.Id != excludeId.Value);
            }

            return await _roleManager.Roles
                .AnyAsync(r => r.Name!.ToLower() == name.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking role name existence: {RoleName}", name);
            throw new AuthException("Failed to check role name existence", innerException: ex);
        }
    }

    /// <summary>
    /// Get roles with filtering, sorting and pagination
    /// </summary>
    public async Task<(List<RoleEntity> Roles, int TotalCount)> GetRolesAsync(RoleQueryRequest query)
    {
        try
        {
            var queryable = _roleManager.Roles.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(query.SearchTerm))
                queryable = queryable.Where(r => r.Name!.Contains(query.SearchTerm));

            // Apply sorting
            if (!string.IsNullOrEmpty(query.SortBy))
            {
                queryable = query.SortBy.ToLower() switch
                {
                    "name" => query.SortDescending ? queryable.OrderByDescending(r => r.Name) : queryable.OrderBy(r => r.Name),
                    "description" => query.SortDescending ? queryable.OrderByDescending(r => r.Description) : queryable.OrderBy(r => r.Description),
                    "created_at" => query.SortDescending ? queryable.OrderByDescending(r => r.CreatedAt) : queryable.OrderBy(r => r.CreatedAt),
                    "updated_at" => query.SortDescending ? queryable.OrderByDescending(r => r.UpdatedAt) : queryable.OrderBy(r => r.UpdatedAt),
                    _ => queryable.OrderBy(r => r.Name) // Default sort by name
                };
            }
            else
            {
                // Default sorting by name if no SortBy specified
                queryable = queryable.OrderBy(r => r.Name);
            }

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
    /// Check if permission name exists (case-insensitive)
    /// </summary>
    public async Task<bool> PermissionNameExistsAsync(string name, Guid? excludeId = null)
    {
        try
        {
            if (excludeId.HasValue)
            {
                return await _context.Permissions
                    .AnyAsync(p => p.Name.ToLower() == name.ToLower() && p.Id != excludeId.Value);
            }

            return await _context.Permissions
                .AnyAsync(p => p.Name.ToLower() == name.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission name existence: {PermissionName}", name);
            throw new AuthException("Failed to check permission name existence", innerException: ex);
        }
    }

    /// <summary>
    /// Get permissions with filtering, sorting and pagination
    /// </summary>
    public async Task<(List<PermissionEntity> Permissions, int TotalCount)> GetPermissionsAsync(PermissionQueryRequest query)
    {
        try
        {
            var queryable = _context.Permissions.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(query.SearchTerm))
                queryable = queryable.Where(p => EF.Functions.Like(p.Name, $"%{query.SearchTerm}%"));

            // Apply sorting
            if (!string.IsNullOrEmpty(query.SortBy))
            {
                queryable = query.SortBy.ToLower() switch
                {
                    "name" => query.SortDescending ? queryable.OrderByDescending(p => p.Name) : queryable.OrderBy(p => p.Name),
                    "description" => query.SortDescending ? queryable.OrderByDescending(p => p.Description) : queryable.OrderBy(p => p.Description),
                    "created_at" => query.SortDescending ? queryable.OrderByDescending(p => p.CreatedAt) : queryable.OrderBy(p => p.CreatedAt),
                    "updated_at" => query.SortDescending ? queryable.OrderByDescending(p => p.UpdatedAt) : queryable.OrderBy(p => p.UpdatedAt),
                    _ => queryable.OrderBy(p => p.Name) // Default sort by name
                };
            }
            else
            {
                // Default sorting by name if no SortBy specified
                queryable = queryable.OrderBy(p => p.Name);
            }

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
    /// Check if account already has the specified role by name using AnyAsync for optimal performance
    /// </summary>
    public async Task<bool> RoleAlreadyAssignedAsync(AccountEntity account, string roleName)
    {
        try
        {
            return await _userManager.IsInRoleAsync(account, roleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if role is already assigned by name: AccountId={AccountId}, RoleName={RoleName}", account.Id, roleName);
            throw new AuthException("Failed to check if role is already assigned by name", innerException: ex);
        }
    }

    /// <summary>
    /// Assign role to account
    /// </summary>
    public async Task<AccountRoleEntity> AssignRoleToAccountAsync(AccountEntity account, RoleEntity role)
    {
        try
        {
            var result = await _userManager.AddToRoleAsync(account, role.Name!);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new AuthException($"Failed to assign role to account: {errors}");
            }

            _logger.LogInformation("Successfully assigned role {RoleName} to account {AccountId}", role.Name, account.Id);
            return new AccountRoleEntity
            {
                UserId = account.Id,
                RoleId = role.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to account: AccountId={AccountId}, RoleId={RoleId}", account.Id, role.Id);
            throw new AuthException("Failed to assign role to account", innerException: ex);
        }
    }

    /// <summary>
    /// Remove role from account
    /// </summary>
    public async Task<bool> RemoveRoleFromAccountAsync(AccountEntity account, RoleEntity role)
    {
        try
        {
            var result = await _userManager.RemoveFromRoleAsync(account, role.Name!);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new AuthException($"Failed to remove role from account: {errors}");
            }

            _logger.LogInformation("Successfully removed role {RoleName} from account {AccountId}", role.Name, account.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from account: AccountId={AccountId}, RoleId={RoleId}", account.Id, role.Id);
            throw new AuthException("Failed to remove role from account", innerException: ex);
        }
    }

    /// <summary>
    /// Get account roles
    /// </summary>
    public async Task<List<RoleEntity>> GetAccountRolesAsync(AccountEntity account)
    {
        try
        {
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
            _logger.LogError(ex, "Error getting account roles: {AccountId}", account.Id);
            throw new AuthException("Failed to get account roles", innerException: ex);
        }
    }

    /// <summary>
    /// Get accounts by role
    /// </summary>
    public async Task<List<AccountEntity>> GetAccountsByRoleAsync(RoleEntity role)
    {
        try
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            return usersInRole.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts by role: {RoleId}", role.Id);
            throw new AuthException("Failed to get accounts by role", innerException: ex);
        }
    }

    #endregion

    #region Account-Permission Operations

    /// <summary>
    /// Get all permissions for an account through its roles
    /// </summary>
    public async Task<List<string>> GetAccountPermissionsAsync(AccountEntity account)
    {
        try
        {
            var accountRoles = await GetAccountRolesAsync(account);
            var tasks = accountRoles.Select(role => GetRolePermissionsAsync(role.Id));

            var rolePermissions = await Task.WhenAll(tasks);

            var permissions = rolePermissions
                .SelectMany(p => p)
                .Select(p => p.Name)
                .Distinct()
                .ToList();

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account permissions: AccountId={AccountId}", account.Id);
            throw new AuthException("Failed to get account permissions", innerException: ex);
        }
    }

    #endregion

    #region Role-Permission Operations

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
                PermissionId = permissionId
            };
            var result = await _context.RolePermissions.AddAsync(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully assigned permission {PermissionId} to role {RoleId}", permissionId, roleId);
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
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission == null) return false;

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully removed permission {PermissionId} from role {RoleId}", permissionId, roleId);
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

    #endregion

    #region Authentication Operations

    /// <summary>
    /// Validate credentials
    /// </summary>
    public async Task<bool> ValidateCredentialsAsync(AccountEntity account, string password)
    {
        try
        {
            return await _userManager.CheckPasswordAsync(account, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credentials for email: {Email}", account.Email);
            throw new AuthException("Failed to validate credentials", innerException: ex);
        }
    }

    /// <summary>
    /// Validate credentials with lockout support
    /// </summary>
    public async Task<bool> ValidateCredentialsWithLockoutAsync(AccountEntity account, string password)
    {
        try
        {
            // Check password
            var isValid = await ValidateCredentialsAsync(account, password);

            if (isValid)
            {
                // Reset failed access count on successful login
                await _userManager.ResetAccessFailedCountAsync(account);
                _logger.LogInformation("Successful login for account: {Email}", account.Email);
            }
            else
            {
                // Increment failed access count
                await _userManager.AccessFailedAsync(account);
                _logger.LogWarning("Failed login attempt for account: {Email}", account.Email);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credentials with lockout for email: {Email}", account.Email);
            throw new AuthException("Failed to validate credentials", innerException: ex);
        }
    }

    /// <summary>
    /// Check if account is locked out
    /// </summary>
    public async Task<bool> IsAccountLockedOutAsync(AccountEntity account)
    {
        try
        {
            return await _userManager.IsLockedOutAsync(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking lockout status for account: {AccountId}", account.Id);
            throw new AuthException("Failed to check lockout status", innerException: ex);
        }
    }

    /// <summary>
    /// Check if account has external login providers (Google, Facebook, etc.)
    /// </summary>
    public async Task<bool> HasExternalLoginAsync(AccountEntity account)
    {
        try
        {
            var logins = await _userManager.GetLoginsAsync(account);
            return logins.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking external login status for account: {AccountId}", account.Id);
            throw new AuthException("Failed to check external login status", innerException: ex);
        }
    }

    /// <summary>
    /// Change password
    /// </summary>
    public async Task<bool> ChangePasswordAsync(AccountEntity account, string newPassword)
    {
        try
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(account);
            var result = await _userManager.ResetPasswordAsync(account, token, newPassword);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for account: {AccountId}", account.Id);
            throw new AuthException("Failed to change password", innerException: ex);
        }
    }

    /// <summary>
    /// Reset password using Identity reset token
    /// </summary>
    public async Task<bool> ResetPasswordWithTokenAsync(AccountEntity account, string resetToken, string newPassword)
    {
        try
        {
            var result = await _userManager.ResetPasswordAsync(account, resetToken, newPassword);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password with token for email: {Email}", account.Email);
            throw new AuthException("Failed to reset password with token", innerException: ex);
        }
    }

    /// <summary>
    /// Lock account
    /// </summary>
    public async Task<bool> LockAccountAsync(AccountEntity account)
    {
        try
        {
            var result = await _userManager.SetLockoutEndDateAsync(account, DateTimeOffset.MaxValue);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking account: {AccountId}", account.Id);
            throw new AuthException("Failed to lock account", innerException: ex);
        }
    }

    /// <summary>
    /// Unlock account
    /// </summary>
    public async Task<bool> UnlockAccountAsync(AccountEntity account)
    {
        try
        {
            var result = await _userManager.SetLockoutEndDateAsync(account, null);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking account: {AccountId}", account.Id);
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
            var account = await GetAccountByEmailAsync(email);
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
    public async Task<(bool Found, string Email, string Token)> GeneratePasswordResetTokenByPhoneAsync(string phoneNumber)
    {
        try
        {
            var account = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (account == null)
            {
                return (false, string.Empty, string.Empty);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(account);
            return (true, account.Email ?? string.Empty, token);
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

    #region Refresh Token Operations

    /// <summary>
    /// Create new refresh token for account
    /// </summary>
    public async Task<RefreshTokenEntity> CreateRefreshTokenAsync(Guid accountId)
    {
        try
        {
            // Ensure only one active refresh token per account
            var existingTokens = await _context.RefreshTokens
                .Where(rt => rt.AccountId == accountId)
                .ToListAsync();
            if (existingTokens.Count > 0)
            {
                _context.RefreshTokens.RemoveRange(existingTokens);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} existing refresh tokens for account: {AccountId}", existingTokens.Count, accountId);
            }

            // Generate a unique refresh token with retries to avoid rare collisions
            const int maxAttempts = 5;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var candidateToken = GenerateRefreshToken();

                // Fast existence check to reduce likelihood of hitting DB unique constraint
                var exists = await _context.RefreshTokens.AnyAsync(rt => rt.Token == candidateToken);
                if (exists)
                {
                    _logger.LogWarning("Collision detected for refresh token on attempt {Attempt}. Retrying...", attempt);
                    continue;
                }

                var refreshToken = new RefreshTokenEntity
                {
                    Id = Guid.NewGuid(),
                    Token = candidateToken,
                    AccountId = accountId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays())
                };

                _context.RefreshTokens.Add(refreshToken);
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created refresh token for account: {AccountId}", accountId);
                    return refreshToken;
                }
                catch (DbUpdateException dbEx)
                {
                    // Handle race-condition collision on unique index
                    if (dbEx.InnerException?.Message.Contains("IX_RefreshTokens_Token") == true)
                    {
                        _logger.LogWarning(dbEx, "Unique index collision for refresh token on attempt {Attempt}. Retrying...", attempt);
                        _context.Entry(refreshToken).State = EntityState.Detached;
                        continue;
                    }
                    throw;
                }
            }

            // If we got here, something is abnormal
            throw new InvalidOperationException("Failed to generate a unique refresh token after multiple attempts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refresh token for account: {AccountId}", accountId);
            throw new AuthException("Failed to create refresh token", innerException: ex);
        }
    }

    /// <summary>
    /// Generate secure refresh token
    /// </summary>
    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Get refresh token expiration time in days
    /// </summary>
    public int GetRefreshTokenExpirationDays()
    {
        if (int.TryParse(_configuration["Jwt:RefreshTokenExpirationDays"], out int days))
        {
            return Math.Max(days, 1); // Minimum 1 day
        }
        return 7; // Default 7 days
    }

    /// <summary>
    /// Validate refresh token and return account
    /// </summary>
    public async Task<(bool IsValid, AccountEntity? Account, RefreshTokenEntity? Token)> ValidateRefreshTokenAsync(string token)
    {
        try
        {
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.Account)
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token not found: {Token}", token.Length > 8 ? token[..8] + "..." : token);
                return (false, null, null);
            }

            if (refreshToken.IsExpired)
            {
                _logger.LogWarning("Refresh token is expired: {TokenId}", refreshToken.Id);
                await DeleteRefreshTokenAsync(token);
                return (false, null, null);
            }

            _logger.LogInformation("Refresh token validated successfully: {TokenId}", refreshToken.Id);
            return (true, refreshToken.Account, refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token");
            throw new AuthException("Failed to validate refresh token", innerException: ex);
        }
    }

    /// <summary>
    /// Delete a refresh token by raw token string
    /// </summary>
    public async Task<bool> DeleteRefreshTokenAsync(string token)
    {
        try
        {
            var entity = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
            if (entity == null)
            {
                _logger.LogWarning("Delete skipped. Refresh token not found: {TokenPrefix}", token.Length > 8 ? token[..8] + "..." : token);
                return false;
            }
            _context.RefreshTokens.Remove(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted refresh token by string: {TokenId}", entity.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting refresh token by string");
            throw new AuthException("Failed to delete refresh token", innerException: ex);
        }
    }

    #endregion

    #region External Login Operations

    /// <summary>
    /// Create user
    /// </summary>
    public async Task<IdentityResult> CreateAccountAsync(AccountEntity user)
    {
        try
        {
            return await _userManager.CreateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Email}", user.Email);
            throw new AuthException("Failed to create user", innerException: ex);
        }
    }

    /// <summary>
    /// Check if user has external login
    /// </summary>
    public async Task<bool> HasExternalLoginAsync(Guid userId, string loginProvider, string providerKey)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var logins = await _userManager.GetLoginsAsync(user);
            return logins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking external login for user: {UserId}", userId);
            throw new AuthException("Failed to check external login", innerException: ex);
        }
    }

    /// <summary>
    /// Add external login to user
    /// </summary>
    public async Task AddExternalLoginAsync(Guid userId, string loginProvider, string providerKey)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new AuthException("User not found");
            }

            var loginInfo = new UserLoginInfo(loginProvider, providerKey, loginProvider);
            var result = await _userManager.AddLoginAsync(user, loginInfo);

            if (!result.Succeeded)
            {
                throw new AuthException($"Failed to add external login: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding external login for user: {UserId}", userId);
            throw new AuthException("Failed to add external login", innerException: ex);
        }
    }

    #endregion
}
