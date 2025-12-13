namespace CodeBeam.UltimateAuth.Core.Domain
{
    /// <summary>
    /// Represents a single authentication session belonging to a user.
    /// Sessions are immutable, security-critical units used for validation,
    /// sliding expiration, revocation, and device analytics.
    /// </summary>
    public interface ISession<TUserId>
    {
        /// <summary>
        /// Gets the unique identifier of the session.
        /// </summary>
        AuthSessionId SessionId { get; }

        /// <summary>
        /// Gets the identifier of the user who owns this session.
        /// </summary>
        TUserId UserId { get; }

        /// <summary>
        /// Gets the timestamp when this session was originally created.
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the timestamp when the session becomes invalid due to expiration.
        /// </summary>
        DateTime ExpiresAt { get; }

        /// <summary>
        /// Gets the timestamp of the last successful usage.
        /// Used when evaluating sliding expiration policies.
        /// </summary>
        DateTime? LastSeenAt { get; }

        /// <summary>
        /// Gets a value indicating whether this session has been explicitly revoked.
        /// </summary>
        bool IsRevoked { get; }

        /// <summary>
        /// Gets the timestamp when the session was revoked, if applicable.
        /// </summary>
        DateTime? RevokedAt { get; }

        /// <summary>
        /// Gets the user's security version at the moment of session creation.
        /// If the stored version does not match the user's current version,
        /// the session becomes invalid (e.g., after password or MFA reset).
        /// </summary>
        long SecurityVersionAtCreation { get; }

        /// <summary>
        /// Gets metadata describing the client device that created the session.
        /// Includes platform, OS, IP address, fingerprint, and more.
        /// </summary>
        DeviceInfo Device { get; }

        /// <summary>
        /// Gets session-scoped metadata used for application-specific extensions,
        /// such as tenant data, app version, locale, or CSRF tokens.
        /// </summary>
        SessionMetadata Metadata { get; }

        /// <summary>
        /// Computes the effective runtime state of the session (Active, Expired,
        /// Revoked, SecurityVersionMismatch, etc.) based on the provided timestamp.
        /// </summary>
        /// <param name="now">Current timestamp used for comparisons.</param>
        /// <returns>The evaluated <see cref="SessionState"/> of this session.</returns>
        SessionState GetState(DateTime now);
    }
}
