using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    /// <summary>
    /// Represents the result of a session issuance operation.
    /// </summary>
    public sealed class IssuedSession<TUserId>
    {
        /// <summary>
        /// The issued domain session.
        /// </summary>
        public required ISession<TUserId> Session { get; init; }

        /// <summary>
        /// Opaque session identifier returned to the client.
        /// </summary>
        public required string OpaqueSessionId { get; init; }

        /// <summary>
        /// Indicates whether this issuance is metadata-only
        /// (used in SemiHybrid mode).
        /// </summary>
        public bool IsMetadataOnly { get; init; }
    }

}
