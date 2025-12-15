using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents an authentication-domain exception thrown when an operation attempts
    /// to use a session that has been explicitly revoked by the user, administrator,
    /// or by system-driven security policies.
    ///
    /// A revoked session is permanently invalid and cannot be refreshed, validated,
    /// or used to obtain new tokens. Revocation typically occurs during actions such as
    /// logout, device removal, or administrative account lockdown.
    ///
    /// This exception is raised in scenarios where a caller assumes the session is active
    /// but the underlying session state indicates <see cref="SessionState.Revoked"/>.
    /// </summary>
    public sealed class UAuthSessionRevokedException : UAuthSessionException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthSessionRevokedException"/> class.
        /// </summary>
        /// <param name="sessionId">The identifier of the revoked session.</param>
        public UAuthSessionRevokedException(AuthSessionId sessionId) : base(sessionId, $"Session '{sessionId}' has been revoked.")
        {
        }
    }
}
