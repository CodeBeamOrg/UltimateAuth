using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Authorization;

public class UAuthPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public UAuthPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public async Task<AuthorizationPolicy?> GetPolicyAsync(
        string policyName)
    {
        var configured = await _fallback.GetPolicyAsync(policyName);

        if (configured is not null)
            return configured;

        return new AuthorizationPolicyBuilder()
            .AddRequirements(new UAuthActionRequirement(policyName))
            .Build();
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();
}
