using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Policies.Abstractions;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class AuthorizationService : IAuthorizationService
{
    private readonly IAccessPolicyProvider _policyProvider;
    private readonly IAccessAuthority _accessAuthority;

    public AuthorizationService(IAccessPolicyProvider policyProvider, IAccessAuthority accessAuthority)
    {
        _policyProvider = policyProvider;
        _accessAuthority = accessAuthority;
    }

    public Task<AuthorizationResult> AuthorizeAsync(AccessContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var policies = _policyProvider.GetPolicies(context);
        var decision = _accessAuthority.Decide(context, policies);

        if (decision.RequiresReauthentication)
            return Task.FromResult(AuthorizationResult.ReauthRequired());

        return Task.FromResult(
            decision.IsAllowed
                ? AuthorizationResult.Allow()
                : AuthorizationResult.Deny(decision.DenyReason)
        );
    }

}
