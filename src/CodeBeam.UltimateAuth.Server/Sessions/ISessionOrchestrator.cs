using CodeBeam.UltimateAuth.Core.Contexts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Sessions
{
    /// <summary>
    /// Orchestrates session, chain, and root lifecycles
    /// according to UltimateAuth security rules.
    /// </summary>
    public interface ISessionOrchestrator<TUserId>
    {
        /// <summary>
        /// Creates a new login session (initial authentication).
        /// </summary>
        Task<IssuedSession<TUserId>> CreateLoginSessionAsync(
            SessionIssueContext<TUserId> context);

        /// <summary>
        /// Revokes a single session.
        /// </summary>
        Task RevokeSessionAsync(
            string? tenantId,
            AuthSessionId sessionId,
            DateTime at);

        /// <summary>
        /// Revokes all sessions of a user (global logout).
        /// </summary>
        Task RevokeAllSessionsAsync(
            string? tenantId,
            TUserId userId,
            DateTime at);
    }
}
