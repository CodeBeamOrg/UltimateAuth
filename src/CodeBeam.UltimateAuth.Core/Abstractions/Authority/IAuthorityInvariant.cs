using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

// TODO: Add ClockSkewInvariant to handle cases where client and server clocks are not synchronized, which can lead to valid tokens being rejected due to "not valid yet" or "expired" errors. This invariant would check the token's "nbf" (not before) and "exp" (expiration) claims against the current time, allowing for a configurable clock skew (e.g., 5 minutes) to accommodate minor discrepancies in system clocks.
public interface IAuthorityInvariant
{
    [Obsolete("Implement DecideAsync instead.")]
    AccessDecisionResult Decide(AuthContext context)
        => throw new NotSupportedException("Implement DecideAsync for asynchronous invariant evaluation.");

    ValueTask<AccessDecisionResult> DecideAsync(AuthContext context, CancellationToken ct = default)
    {
#pragma warning disable CS0618
        return ValueTask.FromResult(Decide(context));
#pragma warning restore CS0618
    }
}
