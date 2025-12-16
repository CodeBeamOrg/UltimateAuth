using CodeBeam.UltimateAuth.Core.Domain;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed class OpaqueTokenRecord
    {
        public string TokenHash { get; init; } = default!;
        public string UserId { get; init; } = default!;
        public string? TenantId { get; init; }
        public AuthSessionId? SessionId { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
        public bool IsRevoked { get; init; }
        public DateTimeOffset? RevokedAt { get; init; }
        public IReadOnlyCollection<Claim> Claims { get; init; } = Array.Empty<Claim>();
    }

}
