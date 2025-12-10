namespace CodeBeam.UltimateAuth.Core.Domain
{
    public interface ISessionChain<TUserId>
    {
        ChainId ChainId { get; }

        /// <summary>
        /// The user who owns this device/login-chain.
        /// </summary>
        TUserId UserId { get; }

        /// <summary>
        /// Unique identifier of the tenant associated with the current context.
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// Number of refresh rotations performed.
        /// </summary>
        int RotationCount { get; }

        /// <summary>
        /// Root.SecurityVersion at chain creation time.
        /// Used to invalidate whole chains when user-level security changes.
        /// </summary>
        long SecurityVersionAtCreation { get; }

        /// <summary>
        /// Claims snapshot for offline / WASM scenarios.
        /// </summary>
        IReadOnlyDictionary<string, object>? ClaimsSnapshot { get; }

        /// <summary>
        /// Rotation history. Only newest session is active.
        /// </summary>
        IReadOnlyList<ISession<TUserId>> Sessions { get; }

        /// <summary>
        /// Whether this chain is revoked (device-level logout).
        /// </summary>
        bool IsRevoked { get; }
        DateTime? RevokedAt { get; }
    }
}
