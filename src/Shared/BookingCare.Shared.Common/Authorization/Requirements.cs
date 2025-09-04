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

