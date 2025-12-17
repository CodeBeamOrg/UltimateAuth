namespace CodeBeam.UltimateAuth.Core.Domain
{
    /// <summary>
    /// Represents the root container for all authentication session chains of a user.
    /// A session root is tenant-scoped and acts as the authoritative security boundary,
    /// controlling global revocation, security versioning, and device/login families.
    /// </summary>
    public interface ISessionRoot<TUserId>
    {
        /// <summary>
        /// Gets the tenant identifier associated with this session root.
        /// Used to isolate authentication domains in multi-tenant systems.
        /// </summary>
        string? TenantId { get; }

        /// <summary>
        /// Gets the identifier of the user who owns this session root.
        /// Each user has one root per tenant.
        /// </summary>
        TUserId UserId { get; }

        /// <summary>
        /// Gets a value indicating whether the entire session root is revoked.
        /// When true, all chains and sessions belonging to this root are invalid,
        /// regardless of their individual states.
        /// </summary>
        bool IsRevoked { get; }

        /// <summary>
        /// Gets the timestamp when the session root was revoked, if applicable.
        /// </summary>
        DateTimeOffset? RevokedAt { get; }

        /// <summary>
        /// Gets the current security version of the user within this tenant.
        /// Incrementing this value invalidates all sessions, even if they are still active.
        /// Common triggers include password reset, MFA reset, and account recovery.
        /// </summary>
        long SecurityVersion { get; }

        /// <summary>
        /// Gets the complete set of session chains associated with this root.
        /// Each chain represents a device or login-family (browser instance, mobile app, etc.).
        /// The root is immutable; modifications must go through SessionService or SessionStore.
        /// </summary>
        IReadOnlyList<ISessionChain<TUserId>> Chains { get; }

        /// <summary>
        /// Gets the timestamp when this root structure was last updated.
        /// Useful for caching, concurrency handling, and incremental synchronization.
        /// </summary>
        DateTimeOffset LastUpdatedAt { get; }

        ISessionRoot<TUserId> AttachChain(ISessionChain<TUserId> chain, DateTimeOffset at);

        ISessionRoot<TUserId> Revoke(DateTimeOffset at);
    }
}
