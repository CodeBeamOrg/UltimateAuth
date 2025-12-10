using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Models
{
    /// <summary>
    /// Represents the outcome of validating a session, including the resolved session,
    /// its chain and root structures, and the computed validation state.
    /// </summary>
    /// <remarks>
    /// Session, Chain and Root may be null if validation fails or if the session
    /// does not exist. State always indicates the final resolved status.
    /// </remarks>
    public sealed class SessionValidationResult<TUserId>
    {
        /// <summary>
        /// The resolved session instance, or null if the session was not found.
        /// </summary>
        public ISession<TUserId>? Session { get; init; }

        /// <summary>
        /// The session chain that owns the session, or null if unavailable.
        /// </summary>
        public ISessionChain<TUserId>? Chain { get; init; }

        /// <summary>
        /// The session root associated with the user, or null if unavailable.
        /// </summary>
        public ISessionRoot<TUserId>? Root { get; init; }

        /// <summary>
        /// The final computed validation state for the session.
        /// </summary>
        public SessionState State { get; init; }
    }
}
