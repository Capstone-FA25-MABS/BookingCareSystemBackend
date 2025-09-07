using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Repositories;

namespace BookingCare.Services.Auth.Services;

/// <summary>
/// Service for initializing default data (permissions, roles, role-permissions)
/// </summary>
public class DataInitializationService
{
    private readonly IAuthRepository _authRepository;
    private readonly ILogger<DataInitializationService> _logger;

    public DataInitializationService(
        IAuthRepository authRepository,
        ILogger<DataInitializationService> logger)
    {
        _authRepository = authRepository;
        _logger = logger;
    }

    /// <summary>
    /// Initialize default permissions, roles and role-permissions
    /// </summary>
    public async Task InitializeDefaultDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting data initialization...");

            // Step 1: Create default permissions
            await CreateDefaultPermissionsAsync();

            // Step 2: Create default roles
            await CreateDefaultRolesAsync();

            // Step 3: Assign permissions to roles
            await AssignDefaultPermissionsToRolesAsync();

            _logger.LogInformation("Data initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data initialization");
            throw;
        }
    }

    /// <summary>
    /// Create default permissions
    /// </summary>
    private async Task CreateDefaultPermissionsAsync()
    {
        var defaultPermissions = new List<CreatePermissionRequest>
        {
            // Patient management permissions
            new() { Name = "Patient.Read", Description = "Read patient information" },
            new() { Name = "Patient.Create", Description = "Create new patient" },
            new() { Name = "Patient.Update", Description = "Update patient information" },
            new() { Name = "Patient.Delete", Description = "Delete patient" },
            
            // Role management permissions
            new() { Name = "Role.Read", Description = "Read role information" },
            new() { Name = "Role.Create", Description = "Create new role" },
            new() { Name = "Role.Update", Description = "Update role information" },
            new() { Name = "Role.Delete", Description = "Delete role" },
            new() { Name = "Role.Assign", Description = "Assign role to patient" },
            
            // Permission management permissions
            new() { Name = "Permission.Read", Description = "Read permission information" },
            new() { Name = "Permission.Create", Description = "Create new permission" },
            new() { Name = "Permission.Update", Description = "Update permission information" },
            new() { Name = "Permission.Delete", Description = "Delete permission" },
            new() { Name = "Permission.Assign", Description = "Assign permission to role" },
            
            // Content management permissions
            new() { Name = "Content.Read", Description = "Read content" },
            new() { Name = "Content.Create", Description = "Create content" },
            new() { Name = "Content.Update", Description = "Update content" },
            new() { Name = "Content.Delete", Description = "Delete content" },
            new() { Name = "Content.Moderate", Description = "Moderate content" }
        };

        foreach (var permission in defaultPermissions)
        {
            try
            {
                if (!await _authRepository.PermissionNameExistsAsync(permission.Name))
                {
                    var permissionEntity = new Models.Entities.PermissionEntity
                    {
                        Name = permission.Name,
                        Description = permission.Description,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    await _authRepository.CreatePermissionAsync(permissionEntity);
                    _logger.LogInformation("Created permission: {PermissionName}", permission.Name);
                }
                else
                {
                    _logger.LogDebug("Permission already exists: {PermissionName}", permission.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission: {PermissionName}", permission.Name);
            }
        }
    }

    /// <summary>
    /// Create default roles
    /// </summary>
    private async Task CreateDefaultRolesAsync()
    {
        var defaultRoles = new List<CreateRoleRequest>
        {
            new() { Name = "Patient", Description = "Regular patient with basic permissions" },
            new() { Name = "Doctor", Description = "Doctor with medical management permissions" },
            new() { Name = "Admin", Description = "Administrator with full system permissions" }
        };

        foreach (var role in defaultRoles)
        {
            try
            {
                if (!await _authRepository.RoleNameExistsAsync(role.Name))
                {
                    var roleEntity = new Models.Entities.RoleEntity
                    {
                        Name = role.Name,
                        Description = role.Description,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    await _authRepository.CreateRoleAsync(roleEntity);
                    _logger.LogInformation("Created role: {RoleName}", role.Name);
                }
                else
                {
                    _logger.LogDebug("Role already exists: {RoleName}", role.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role: {RoleName}", role.Name);
            }
        }
    }

    /// <summary>
    /// Assign default permissions to roles
    /// </summary>
    private async Task AssignDefaultPermissionsToRolesAsync()
    {
        try
        {
            // Get roles
            var patientRole = await _authRepository.GetRoleByNameAsync("Patient");
            var doctorRole = await _authRepository.GetRoleByNameAsync("Doctor");
            var adminRole = await _authRepository.GetRoleByNameAsync("Admin");

            if (patientRole == null || doctorRole == null || adminRole == null)
            {
                _logger.LogWarning("One or more default roles not found. Skipping permission assignment.");
                return;
            }

            // Patient role permissions: Basic read permissions
            var patientPermissions = new[] { "Patient.Read", "Content.Read" };
            await AssignPermissionsToRoleAsync(patientRole.Id, patientPermissions, "Patient");

            // Doctor role permissions: Medical management + patient read
            var doctorPermissions = new[] { 
                "Patient.Read", "Content.Read", "Content.Create", "Content.Update", 
                "Content.Delete", "Content.Moderate" 
            };
            await AssignPermissionsToRoleAsync(doctorRole.Id, doctorPermissions, "Doctor");

            // Admin role permissions: Everything
            var adminPermissions = new[] { 
                "Patient.Read", "Patient.Create", "Patient.Update", "Patient.Delete",
                "Role.Read", "Role.Create", "Role.Update", "Role.Delete", "Role.Assign",
                "Permission.Read", "Permission.Create", "Permission.Update", "Permission.Delete", "Permission.Assign",
                "Content.Read", "Content.Create", "Content.Update", "Content.Delete", "Content.Moderate"
            };
            await AssignPermissionsToRoleAsync(adminRole.Id, adminPermissions, "Admin");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning default permissions to roles");
        }
    }

    /// <summary>
    /// Assign permissions to a specific role
    /// </summary>
    private async Task AssignPermissionsToRoleAsync(Guid roleId, string[] permissionNames, string roleName)
    {
        foreach (var permissionName in permissionNames)
        {
            try
            {
                var permission = await _authRepository.GetPermissionByNameAsync(permissionName);
                if (permission != null)
                {
                    // Check if permission is already assigned
                    if (!await _authRepository.RoleHasPermissionAsync(roleId, permission.Id))
                    {
                        await _authRepository.AssignPermissionToRoleAsync(roleId, permission.Id);
                        _logger.LogInformation("Assigned permission '{PermissionName}' to role '{RoleName}'", permissionName, roleName);
                    }
                    else
                    {
                        _logger.LogDebug("Permission '{PermissionName}' already assigned to role '{RoleName}'", permissionName, roleName);
                    }
                }
                else
                {
                    _logger.LogWarning("Permission not found: {PermissionName}", permissionName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning permission '{PermissionName}' to role '{RoleName}'", permissionName, roleName);
            }
        }
    }
}
