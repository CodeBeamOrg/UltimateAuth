using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Provides high-level session lifecycle operations such as creation, refresh, validation, and revocation.
    /// </summary>
    /// <typeparam name="TUserId">The type used to uniquely identify the user.</typeparam>
    public interface IUAuthSessionService<TUserId>
    {
        Task<SessionValidationResult<TUserId>> ValidateSessionAsync(string? tenantId, AuthSessionId sessionId, DateTime at);

        Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsAsync(string? tenantId, TUserId userId);

        Task<IReadOnlyList<ISession<TUserId>>> GetSessionsAsync(string? tenantId, ChainId chainId);

        Task<ISession<TUserId>?> GetCurrentSessionAsync(string? tenantId, AuthSessionId sessionId);

        Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTime at);

        Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTime at);

        Task<ChainId?> ResolveChainIdAsync(string? tenantId, AuthSessionId sessionId);

        Task RevokeAllChainsAsync(string? tenantId, TUserId userId, ChainId? exceptChainId, DateTime at);
        
        // Hard revoke - admin
        Task RevokeRootAsync(string? tenantId, TUserId userId, DateTime at);

        Task<IssuedSession<TUserId>> IssueSessionAfterAuthenticationAsync(string? tenantId, AuthenticatedSessionContext<TUserId> context, CancellationToken cancellationToken = default);
    }
}
