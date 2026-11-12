using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Authorization;

public class UAuthPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;
    private readonly bool _enforceActiveUser;

    public UAuthPolicyProvider(IOptions<AuthorizationOptions> options, bool enforceActiveUser = false)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
        _enforceActiveUser = enforceActiveUser;
    }

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var configured = await _fallback.GetPolicyAsync(policyName);

        if (configured is not null)
            return WithActiveUserRequirement(configured);

        var actionPolicy = new AuthorizationPolicyBuilder()
            .AddRequirements(new UAuthActionRequirement(policyName))
            .Build();

        return WithActiveUserRequirement(actionPolicy);
    }

    public async Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        var policy = await _fallback.GetDefaultPolicyAsync();
        return WithActiveUserRequirement(policy);
    }

    public async Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        var policy = await _fallback.GetFallbackPolicyAsync();

        return policy is null
            ? null
            : WithActiveUserRequirement(policy);
    }

    private AuthorizationPolicy WithActiveUserRequirement(AuthorizationPolicy policy)
    {
        if (!_enforceActiveUser || policy.Requirements.OfType<UAuthActiveUserRequirement>().Any())
        {
            return policy;
        }

        return new AuthorizationPolicy(
            policy.Requirements.Append(new UAuthActiveUserRequirement()),
            policy.AuthenticationSchemes);
    }
}
