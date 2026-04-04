using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface ISessionIssuer
{
    Task<IssuedSession> IssueSessionAsync(SessionIssuanceContext context, CancellationToken cancellationToken = default);

    Task<IssuedSession> RotateSessionAsync(SessionRotationContext context, CancellationToken cancellationToken = default);

    Task<bool> RevokeSessionAsync(TenantKey tenant, AuthSessionId sessionId, DateTimeOffset at, CancellationToken cancellationToken = default);

    Task RevokeChainAsync(TenantKey tenant, SessionChainId chainId, DateTimeOffset at, CancellationToken cancellationToken = default);

    Task RevokeAllChainsAsync(TenantKey tenant, UserKey userKey, SessionChainId? exceptChainId, DateTimeOffset at, CancellationToken ct = default);

    Task RevokeRootAsync(TenantKey tenant, UserKey userKey, DateTimeOffset at,CancellationToken ct = default);
}
