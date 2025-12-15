using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents an authentication-domain exception thrown when a session exists
    /// but is not in the <see cref="SessionState.Active"/> state.
    /// This exception typically occurs during validation or refresh operations when:
    /// - the session is revoked,
    /// - the session has expired,
    /// - the session belongs to a revoked chain,
    /// - or the session is otherwise considered inactive by the runtime state machine.
    /// Only active sessions are eligible for refresh and token issuance.
    /// </summary>
    public sealed class UAuthSessionNotActiveException : UAuthSessionException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthSessionNotActiveException"/> class.
        /// </summary>
        /// <param name="sessionId">The identifier of the session that is not active.</param>
        public UAuthSessionNotActiveException(AuthSessionId sessionId) : base(sessionId, $"Session '{sessionId}' is not active.")
        {
        }
    }
}
