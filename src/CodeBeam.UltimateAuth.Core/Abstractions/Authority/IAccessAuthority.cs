using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IAccessAuthority
{
    [Obsolete("Use DecideAsync instead.")]
    AccessDecision Decide(AccessContext context, IEnumerable<IAccessPolicy> runtimePolicies)
        => throw new NotSupportedException("Synchronous authorization is not supported by this authority.");

    ValueTask<AccessDecision> DecideAsync(AccessContext context, IEnumerable<IAccessPolicy> runtimePolicies, CancellationToken cancellationToken = default)
    {
#pragma warning disable CS0618
        return ValueTask.FromResult(Decide(context, runtimePolicies));
#pragma warning restore CS0618
    }
}
