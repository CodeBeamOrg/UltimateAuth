using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface ISessionIssuer<TUserId>
    {
        Task<IssuedSession<TUserId>> IssueLoginSessionAsync(AuthenticatedSessionContext<TUserId> context, CancellationToken cancellationToken = default);

        Task<IssuedSession<TUserId>> RotateSessionAsync(SessionRotationContext<TUserId> context, CancellationToken cancellationToken = default);

        Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at, CancellationToken cancellationToken = default);

        Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTimeOffset at, CancellationToken cancellationToken = default);

        Task RevokeAllChainsAsync(string? tenantId, TUserId userId, ChainId? exceptChainId, DateTimeOffset at, CancellationToken ct = default);

        Task RevokeRootAsync(string? tenantId, TUserId userId, DateTimeOffset at,CancellationToken ct = default);
    }
}
