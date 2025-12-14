using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Models;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Provides high-level session lifecycle operations such as creation, refresh, validation, and revocation.
    /// </summary>
    /// <typeparam name="TUserId">The type used to uniquely identify the user.</typeparam>
    public interface IUAuthSessionService<TUserId>
    {
        // ---------- READ ----------
        Task<SessionValidationResult<TUserId>> ValidateSessionAsync(
            string? tenantId,
            AuthSessionId sessionId,
            DateTime now);

        Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsAsync(
            string? tenantId,
            TUserId userId);

        Task<IReadOnlyList<ISession<TUserId>>> GetSessionsAsync(
            string? tenantId,
            ChainId chainId);

        Task<ISession<TUserId>?> GetCurrentSessionAsync(
            string? tenantId,
            AuthSessionId sessionId);

        // ---------- WRITE / REVOKE ----------
        Task RevokeSessionAsync(
            string? tenantId,
            AuthSessionId sessionId,
            DateTime at);

        Task RevokeChainAsync(
            string? tenantId,
            ChainId chainId,
            DateTime at);

        Task RevokeRootAsync(
            string? tenantId,
            TUserId userId,
            DateTime at);
    }
}
