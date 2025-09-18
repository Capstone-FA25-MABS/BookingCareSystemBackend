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
            var role = policyName.Substring(RolePrefix.Length);
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new RoleRequirement(role))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }
}

