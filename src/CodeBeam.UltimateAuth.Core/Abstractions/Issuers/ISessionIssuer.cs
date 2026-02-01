using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface ISessionIssuer
{
    Task<IssuedSession> IssueLoginSessionAsync(AuthenticatedSessionContext context, CancellationToken cancellationToken = default);

    Task<IssuedSession> RotateSessionAsync(SessionRotationContext context, CancellationToken cancellationToken = default);

    Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at, CancellationToken cancellationToken = default);

    Task RevokeChainAsync(string? tenantId, SessionChainId chainId, DateTimeOffset at, CancellationToken cancellationToken = default);

    Task RevokeAllChainsAsync(string? tenantId, UserKey userKey, SessionChainId? exceptChainId, DateTimeOffset at, CancellationToken ct = default);

    Task RevokeRootAsync(string? tenantId, UserKey userKey, DateTimeOffset at,CancellationToken ct = default);
}
