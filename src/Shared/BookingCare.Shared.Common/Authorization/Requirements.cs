using Microsoft.AspNetCore.Authorization;

namespace BookingCare.Shared.Common.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionName { get; }
    public PermissionRequirement(string permissionName) => PermissionName = permissionName;
}

public sealed class RoleRequirement : IAuthorizationRequirement
{
    public string RoleName { get; }
    public RoleRequirement(string roleName) => RoleName = roleName;
}

/// <summary>
/// Requirement for multiple roles with OR logic (user must have at least one of the roles)
/// </summary>
public sealed class MultipleRolesRequirement : IAuthorizationRequirement
{
    public string[] RoleNames { get; }
    public MultipleRolesRequirement(params string[] roleNames) => RoleNames = roleNames;
}

