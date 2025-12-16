using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record LogoutRequest
    {
        public string? TenantId { get; init; }
        public AuthSessionId SessionId { get; init; }

        /// <summary>
        /// Optional logical timestamp for the logout operation.
        /// If not provided, the flow service will use DateTime.UtcNow.
        /// </summary>
        public DateTime? At { get; init; }
    }
}
