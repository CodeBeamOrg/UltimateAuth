namespace CodeBeam.UltimateAuth.Core.Domain
{
    /// <summary>
    /// Represents a device- or login-scoped session chain.
    /// A chain groups all rotated sessions belonging to a single logical login
    /// (e.g., a browser instance, mobile app installation, or device fingerprint).
    /// </summary>
    public interface ISessionChain<TUserId>
    {
        /// <summary>
        /// Gets the unique identifier of the session chain.
        /// </summary>
        ChainId ChainId { get; }

        /// <summary>
        /// Gets the identifier of the user who owns this chain.
        /// Each chain represents one device/login family for this user.
        /// </summary>
        TUserId UserId { get; }

        /// <summary>
        /// Gets the number of refresh token rotations performed within this chain.
        /// </summary>
        int RotationCount { get; }

        /// <summary>
        /// Gets the user's security version at the time the chain was created.
        /// If the user's current security version is higher, the entire chain
        /// becomes invalid (e.g., after password or MFA reset).
        /// </summary>
        long SecurityVersionAtCreation { get; }

        /// <summary>
        /// Gets an optional snapshot of claims taken at chain creation time.
        /// Useful for offline clients, WASM apps, and environments where
        /// full user lookup cannot be performed on each request.
        /// </summary>
        ClaimsSnapshot ClaimsSnapshot { get; }

        /// <summary>
        /// Gets the identifier of the currently active authentication session, if one exists.
        /// </summary>
        AuthSessionId? ActiveSessionId { get; }

        /// <summary>
        /// Gets a value indicating whether this chain has been revoked.
        /// Revoking a chain performs a device-level logout, invalidating
        /// all sessions it contains.
        /// </summary>
        bool IsRevoked { get; }

        /// <summary>
        /// Gets the timestamp when the chain was revoked, if applicable.
        /// </summary>
        DateTime? RevokedAt { get; }

        ISessionChain<TUserId> AttachSession(AuthSessionId sessionId);
        ISessionChain<TUserId> RotateSession(AuthSessionId sessionId);
        ISessionChain<TUserId> Revoke(DateTime at);
    }

}
