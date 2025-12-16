using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal interface ISessionOrchestrator<TUserId>
    {
        Task<IssuedSession<TUserId>> CreateLoginSessionAsync(AuthenticatedSessionContext<TUserId> context);

        Task<IssuedSession<TUserId>> RotateSessionAsync(SessionRotationContext<TUserId> context);

        Task<SessionValidationResult<TUserId>> ValidateSessionAsync(SessionValidationContext context);

        Task<ISession<TUserId>?> GetSessionAsync(string? tenantId, AuthSessionId sessionId);

        Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsAsync(string? tenantId, TUserId userId);

        Task<ChainId?> ResolveChainIdAsync(string? tenantId,AuthSessionId sessionId);

        Task<IReadOnlyList<ISession<TUserId>>> GetSessionsAsync(string? tenantId, ChainId chainId);

        Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTime at);

        Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTime at);

        Task RevokeAllChainsAsync(string? tenantId, TUserId userId, ChainId? exceptChainId,DateTime at);

        Task RevokeRootAsync(string? tenantId, TUserId userId, DateTime at);
    }
}
