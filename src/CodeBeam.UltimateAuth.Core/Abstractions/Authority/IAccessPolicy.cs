using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IAccessPolicy
{
    bool AppliesTo(AccessContext context);

    [Obsolete("Implement DecideAsync instead.")]
    AccessDecision Decide(AccessContext context)
        => throw new NotSupportedException("Implement DecideAsync for asynchronous policy evaluation.");

    ValueTask<AccessDecision> DecideAsync(AccessContext context, CancellationToken cancellationToken = default)
    {
#pragma warning disable CS0618
        return ValueTask.FromResult(Decide(context));
#pragma warning restore CS0618
    }
}
