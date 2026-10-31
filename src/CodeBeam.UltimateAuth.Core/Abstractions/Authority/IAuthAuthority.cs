using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IAuthAuthority
{
    [Obsolete("Synchronous authority evaluation is no longer supported. Use DecideAsync instead.")]
    AccessDecisionResult Decide(AuthContext context, IEnumerable<IAuthorityPolicy>? policies = null)
        => throw new NotSupportedException("Synchronous authority evaluation is not supported. Use DecideAsync.");

    ValueTask<AccessDecisionResult> DecideAsync(AuthContext context, IEnumerable<IAuthorityPolicy>? policies = null, CancellationToken ct = default)
    {
#pragma warning disable CS0618
        return ValueTask.FromResult(Decide(context, policies));
#pragma warning restore CS0618
    }
}
