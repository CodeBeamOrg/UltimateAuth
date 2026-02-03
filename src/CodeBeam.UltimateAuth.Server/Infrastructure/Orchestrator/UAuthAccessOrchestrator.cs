using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Policies.Abstractions;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UAuthAccessOrchestrator : IAccessOrchestrator
{
    private readonly IAccessAuthority _authority;
    private readonly IAccessPolicyProvider _policyProvider;

    public UAuthAccessOrchestrator(IAccessAuthority authority, IAccessPolicyProvider policyProvider)
    {
        _authority = authority;
        _policyProvider = policyProvider;
    }

    public async Task ExecuteAsync(AccessContext context, IAccessCommand command, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var policies = _policyProvider.GetPolicies(context);
        var decision = _authority.Decide(context, policies);

        if (!decision.IsAllowed)
            throw new UAuthAuthorizationException(decision.DenyReason);

        if (decision.RequiresReauthentication)
            throw new InvalidOperationException("Requires reauthentication.");

        await command.ExecuteAsync(ct);
    }

    public async Task<TResult> ExecuteAsync<TResult>(AccessContext context, IAccessCommand<TResult> command, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var policies = _policyProvider.GetPolicies(context);
        var decision = _authority.Decide(context, policies);

        if (!decision.IsAllowed)
            throw new UAuthAuthorizationException(decision.DenyReason);

        if (decision.RequiresReauthentication)
            throw new InvalidOperationException("Requires reauthentication.");

        return await command.ExecuteAsync(ct);
    }
}
