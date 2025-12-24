using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BookingCare.Shared.Common.Authorization;

public class DynamicAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private const string PermissionPrefix = "Perm:";
    private const string RolePrefix = "Role:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public DynamicAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var perm = policyName.Substring(PermissionPrefix.Length);
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(perm))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        if (policyName.StartsWith(RolePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var rolesString = policyName.Substring(RolePrefix.Length);

            // Support multiple roles separated by comma: "Role:Admin,Patient" (OR logic)
            if (rolesString.Contains(','))
            {
                var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                // Use custom MultipleRolesRequirement for case-insensitive comparison
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new MultipleRolesRequirement(roles))
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            // Single role
            var singleRolePolicy = new AuthorizationPolicyBuilder()
                .AddRequirements(new RoleRequirement(rolesString))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(singleRolePolicy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }
}

