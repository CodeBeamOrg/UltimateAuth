using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed class LogoutAllRequest
    {
        public string? TenantId { get; init; }
        public AuthSessionId? CurrentSessionId { get; init; }

        /// <summary>
        /// If true, the current session will NOT be revoked.
        /// </summary>
        public bool ExceptCurrent { get; init; }
    }

}
