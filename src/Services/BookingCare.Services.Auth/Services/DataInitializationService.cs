using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Repositories;

namespace BookingCare.Services.Auth.Services;

/// <summary>
/// Constants for permission names and descriptions
/// </summary>
public static class PermissionConstants
{
    // Permission names
    public const string PatientRead = "Patient.Read";
    public const string PatientCreate = "Patient.Create";
    public const string PatientUpdate = "Patient.Update";
    public const string PatientDelete = "Patient.Delete";

    public const string RoleRead = "Role.Read";
    public const string RoleCreate = "Role.Create";
    public const string RoleUpdate = "Role.Update";
    public const string RoleDelete = "Role.Delete";
    public const string RoleAssign = "Role.Assign";

    public const string PermissionRead = "Permission.Read";
    public const string PermissionCreate = "Permission.Create";
    public const string PermissionUpdate = "Permission.Update";
    public const string PermissionDelete = "Permission.Delete";
    public const string PermissionAssign = "Permission.Assign";

    public const string ContentRead = "Content.Read";
    public const string ContentCreate = "Content.Create";
    public const string ContentUpdate = "Content.Update";
    public const string ContentDelete = "Content.Delete";
    public const string ContentModerate = "Content.Moderate";

    // Permission descriptions
    public const string ReadPatientInfo = "Read patient information";
    public const string CreateNewPatient = "Create new patient";
    public const string UpdatePatientInfo = "Update patient information";
    public const string DeletePatient = "Delete patient";

    public const string ReadRoleInfo = "Read role information";
    public const string CreateNewRole = "Create new role";
    public const string UpdateRoleInfo = "Update role information";
    public const string DeleteRole = "Delete role";
    public const string AssignRoleToUser = "Assign role to user";

    public const string ReadPermissionInfo = "Read permission information";
    public const string CreateNewPermission = "Create new permission";
    public const string UpdatePermissionInfo = "Update permission information";
    public const string DeletePermission = "Delete permission";
    public const string AssignPermissionToRole = "Assign permission to role";

    public const string ReadContent = "Read content";
    public const string CreateContent = "Create content";
    public const string UpdateContent = "Update content";
    public const string DeleteContent = "Delete content";
    public const string ModerateContent = "Moderate content";
}

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
            new() { Name = PermissionConstants.PatientRead, Description = PermissionConstants.ReadPatientInfo },
            new() { Name = PermissionConstants.PatientCreate, Description = PermissionConstants.CreateNewPatient },
            new() { Name = PermissionConstants.PatientUpdate, Description = PermissionConstants.UpdatePatientInfo },
            new() { Name = PermissionConstants.PatientDelete, Description = PermissionConstants.DeletePatient },
            
            // Role management permissions
            new() { Name = PermissionConstants.RoleRead, Description = PermissionConstants.ReadRoleInfo },
            new() { Name = PermissionConstants.RoleCreate, Description = PermissionConstants.CreateNewRole },
            new() { Name = PermissionConstants.RoleUpdate, Description = PermissionConstants.UpdateRoleInfo },
            new() { Name = PermissionConstants.RoleDelete, Description = PermissionConstants.DeleteRole },
            new() { Name = PermissionConstants.RoleAssign, Description = PermissionConstants.AssignRoleToUser },
            
            // Permission management permissions
            new() { Name = PermissionConstants.PermissionRead, Description = PermissionConstants.ReadPermissionInfo },
            new() { Name = PermissionConstants.PermissionCreate, Description = PermissionConstants.CreateNewPermission },
            new() { Name = PermissionConstants.PermissionUpdate, Description = PermissionConstants.UpdatePermissionInfo },
            new() { Name = PermissionConstants.PermissionDelete, Description = PermissionConstants.DeletePermission },
            new() { Name = PermissionConstants.PermissionAssign, Description = PermissionConstants.AssignPermissionToRole },
            
            // Content management permissions
            new() { Name = PermissionConstants.ContentRead, Description = PermissionConstants.ReadContent },
            new() { Name = PermissionConstants.ContentCreate, Description = PermissionConstants.CreateContent },
            new() { Name = PermissionConstants.ContentUpdate, Description = PermissionConstants.UpdateContent },
            new() { Name = PermissionConstants.ContentDelete, Description = PermissionConstants.DeleteContent },
            new() { Name = PermissionConstants.ContentModerate, Description = PermissionConstants.ModerateContent }
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
            new() { Name = "Staff", Description = "Hospital/clinic staff with administrative support permissions" },
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
            var staffRole = await _authRepository.GetRoleByNameAsync("Staff");
            var adminRole = await _authRepository.GetRoleByNameAsync("Admin");

            if (patientRole == null || doctorRole == null || staffRole == null || adminRole == null)
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

            // Staff role permissions: Administrative support (create/update but not delete)
            var staffPermissions = new[] {
                "Patient.Read", "Patient.Create", "Patient.Update",
                "Content.Read", "Content.Create", "Content.Update",
                "Role.Read"
            };
            await AssignPermissionsToRoleAsync(staffRole.Id, staffPermissions, "Staff");

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
