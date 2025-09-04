using Microsoft.AspNetCore.Authorization;

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
        if (context.User?.IsInRole(requirement.RoleName) == true) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

