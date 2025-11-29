using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BookingCare.Shared.Common.Authorization;

public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var hasPermission = context.User?.Claims
            .Any(c => c.Type == "permission" && string.Equals(c.Value, requirement.PermissionName, StringComparison.OrdinalIgnoreCase)) == true;
        if (hasPermission) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

public sealed class RoleHandler : AuthorizationHandler<RoleRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
    {
        var hasRole = context.User?.Claims
            .Any(c => c.Type == ClaimTypes.Role && string.Equals(c.Value, requirement.RoleName, StringComparison.OrdinalIgnoreCase)) == true;
        if (hasRole) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for multiple roles requirement with case-insensitive comparison (OR logic)
/// </summary>
public sealed class MultipleRolesHandler : AuthorizationHandler<MultipleRolesRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MultipleRolesRequirement requirement)
    {
        var userRoles = context.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList() ?? new List<string>();

        // Check if user has at least one of the required roles (case-insensitive)
        var hasAnyRole = requirement.RoleNames.Any(requiredRole =>
            userRoles.Any(userRole => string.Equals(userRole, requiredRole, StringComparison.OrdinalIgnoreCase)));

        if (hasAnyRole) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

