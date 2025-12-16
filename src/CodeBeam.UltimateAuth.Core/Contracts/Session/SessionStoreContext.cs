using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    /// <summary>
    /// Context information required by the session store when
    /// creating or rotating sessions.
    /// </summary>
    public sealed class SessionStoreContext<TUserId>
    {
        /// <summary>
        /// The authenticated user identifier.
        /// </summary>
        public required TUserId UserId { get; init; }

        /// <summary>
        /// The tenant identifier, if multi-tenancy is enabled.
        /// </summary>
        public string? TenantId { get; init; }

        /// <summary>
        /// Optional chain identifier.
        /// If null, a new chain should be created.
        /// </summary>
        public ChainId? ChainId { get; init; }

        /// <summary>
        /// Indicates whether the session is metadata-only
        /// (used in SemiHybrid mode).
        /// </summary>
        public bool IsMetadataOnly { get; init; }

        /// <summary>
        /// The UTC timestamp when the session was issued.
        /// </summary>
        public DateTimeOffset IssuedAt { get; init; }

        /// <summary>
        /// Optional device or client identifier.
        /// </summary>
        public DeviceInfo? DeviceInfo { get; init; }
    }
}
