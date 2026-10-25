using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IAuthorityPolicy
{
    bool AppliesTo(AuthContext context);

    [Obsolete("Implement DecideAsync instead.")]
    AccessDecisionResult Decide(AuthContext context)
        => throw new NotSupportedException("Implement DecideAsync for asynchronous policy evaluation.");

    ValueTask<AccessDecisionResult> DecideAsync(AuthContext context, CancellationToken ct = default)
    {
#pragma warning disable CS0618
        return ValueTask.FromResult(Decide(context));
#pragma warning restore CS0618
    }
}
