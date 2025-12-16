using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    /// <summary>
    /// Represents the context in which a session is issued
    /// (login, refresh, reauthentication).
    /// </summary>
    public sealed class AuthenticatedSessionContext<TUserId>
    {
        public string? TenantId { get; init; }
        public required TUserId UserId { get; init; }
        public DeviceInfo DeviceInfo { get; init; }
        public DateTime Now { get; init; }
        public ClaimsSnapshot? Claims { get; init; }
        public SessionMetadata Metadata { get; init; }

        /// <summary>
        /// Optional chain identifier.
        /// If null, a new chain will be created.
        /// If provided, session will be issued under the existing chain.
        /// </summary>
        public ChainId? ChainId { get; init; }

        /// <summary>
        /// Indicates that authentication has already been completed.
        /// This context MUST NOT be constructed from raw credentials.
        /// </summary>
        public bool IsAuthenticated { get; init; } = true;
    }
}
