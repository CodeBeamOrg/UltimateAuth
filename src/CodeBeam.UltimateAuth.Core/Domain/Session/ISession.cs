namespace CodeBeam.UltimateAuth.Core.Domain
{
    public interface ISession<TUserId>
    {
        AuthSessionId SessionId { get; }

        /// <summary>
        /// Owner of this session.
        /// </summary>
        TUserId UserId { get; }

        /// <summary>
        /// Unique identifier of the tenant associated with the current context.
        /// </summary>
        string TenantId { get; }


        /// <summary>
        /// When this session was originally created.
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Expiration timestamp for validation.
        /// </summary>
        DateTime ExpiresAt { get; }

        /// <summary>
        /// Sliding expiration behavior relies on this timestamp.
        /// </summary>
        DateTime LastSeenAt { get; }

        /// <summary>
        /// Whether this session has been revoked individually.
        /// </summary>
        bool IsRevoked { get; }
        DateTime? RevokedAt { get; }

        /// <summary>
        /// Snapshot of root.SecurityVersion at creation time.
        /// Used to invalidate sessions after password/MFA reset.
        /// </summary>
        long SecurityVersionAtCreation { get; }

        /// <summary>
        /// Device-specific metadata (OS, browser, IP, etc.)
        /// </summary>
        DeviceInfo Device { get; }

        /// <summary>
        /// Customizable metadata section for enterprise extensions.
        /// </summary>
        SessionMetadata Metadata { get; }

        /// <summary>
        /// Computes session's runtime state (Active, Expired, Revoked, etc.)
        /// </summary>
        SessionState GetState(DateTime now);
    }

}
