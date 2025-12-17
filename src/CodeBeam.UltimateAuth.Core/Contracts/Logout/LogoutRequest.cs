using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record LogoutRequest
    {
        public string? TenantId { get; init; }
        public AuthSessionId SessionId { get; init; }

        public DateTimeOffset? At { get; init; }
    }
}
