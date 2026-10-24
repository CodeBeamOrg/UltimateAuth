using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UAuthAccessAuthority : IAccessAuthority
{
    private readonly IEnumerable<IAccessInvariant> _invariants;
    private readonly IEnumerable<IAccessPolicy> _globalPolicies;

    public UAuthAccessAuthority(IEnumerable<IAccessInvariant> invariants, IEnumerable<IAccessPolicy> globalPolicies)
    {
        _invariants = invariants ?? Array.Empty<IAccessInvariant>();
        _globalPolicies = globalPolicies ?? Array.Empty<IAccessPolicy>();
    }

    public async ValueTask<AccessDecision> DecideAsync(AccessContext context, IEnumerable<IAccessPolicy> runtimePolicies, CancellationToken ct = default)
    {
        foreach (var invariant in _invariants)
        {
            ct.ThrowIfCancellationRequested();

            var result = await invariant.DecideAsync(context, ct);

            if (!result.IsAllowed)
                return result;
        }

        foreach (var policy in _globalPolicies)
        {
            ct.ThrowIfCancellationRequested();

            if (!policy.AppliesTo(context))
                continue;

            var result = await policy.DecideAsync(context, ct);

            if (!result.IsAllowed)
                return result;

            // Allow here means "no objection", NOT permission.
        }

        bool requiresReauth = false;

        foreach (var policy in runtimePolicies)
        {
            ct.ThrowIfCancellationRequested();

            if (!policy.AppliesTo(context))
                continue;

            var result = await policy.DecideAsync(context, ct);

            if (result.IsDenied)
                return result;

            if (result.RequiresReauthentication)
                requiresReauth = true;
        }

        return requiresReauth
            ? AccessDecision.ReauthenticationRequired()
            : AccessDecision.Allow();
    }
}
