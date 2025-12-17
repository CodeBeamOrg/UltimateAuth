using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed class LogoutAllRequest
    {
        public string? TenantId { get; init; }

        /// <summary>
        /// The current session initiating the logout-all operation.
        /// Used to resolve the active chain when ExceptCurrent is true.
        /// </summary>
        public AuthSessionId? CurrentSessionId { get; init; }

        /// <summary>
        /// If true, the current session will NOT be revoked.
        /// </summary>
        public bool ExceptCurrent { get; init; }

        public DateTimeOffset? At { get; init; }
    }

}
