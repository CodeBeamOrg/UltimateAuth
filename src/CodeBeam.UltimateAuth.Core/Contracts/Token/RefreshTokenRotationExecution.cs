using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record RefreshTokenRotationExecution
{
    public RefreshTokenRotationResult Result { get; init; } = default!;

    // INTERNAL – flow/orchestrator only
    public UserKey? UserKey { get; init; }
    public AuthSessionId? SessionId { get; init; }
    public SessionChainId? ChainId { get; init; }
    public TenantKey Tenant { get; init; }
}
