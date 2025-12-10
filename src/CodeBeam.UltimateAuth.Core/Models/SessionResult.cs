using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Models
{
    // TODO: IsNewChain, IsNewRoot flags?
    /// <summary>
    /// Represents the result of a session operation within UltimateAuth, such as
    /// login or session refresh.
    /// 
    /// A session operation may produce:
    /// - a newly created session,
    /// - an updated session chain (rotation),
    /// - an updated session root (e.g., after adding a new chain).
    /// 
    /// This wrapper provides a unified model so downstream components — such as
    /// token services, event emitters, logging pipelines, or application-level
    /// consumers — can easily access all updated authentication structures.
    /// </summary>
    public sealed class SessionResult<TUserId>
    {
        /// <summary>
        /// Gets the active session produced by the operation.
        /// This is the newest session and the one that should be used when issuing tokens.
        /// </summary>
        public required ISession<TUserId> Session { get; init; }

        /// <summary>
        /// Gets the session chain associated with the session.
        /// The chain may be newly created (login) or updated (session rotation).
        /// </summary>
        public required ISessionChain<TUserId> Chain { get; init; }

        /// <summary>
        /// Gets the user's session root.
        /// This structure may be updated when new chains are added or when security
        /// properties change.
        /// </summary>
        public required ISessionRoot<TUserId> Root { get; init; }
    }
}
