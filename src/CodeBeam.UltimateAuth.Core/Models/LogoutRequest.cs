using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record LogoutRequest
    {
        public string? TenantId { get; init; }
        public AuthSessionId SessionId { get; init; }
    }
}
