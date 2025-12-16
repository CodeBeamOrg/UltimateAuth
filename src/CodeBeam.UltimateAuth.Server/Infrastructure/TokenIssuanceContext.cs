using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed record TokenIssuanceContext
    {
        public string UserId { get; init; } = default!;
        public string? TenantId { get; init; }
        public IReadOnlyCollection<Claim> Claims { get; init; } = Array.Empty<Claim>();
        public string? SessionId { get; init; }
        public DateTime IssuedAt { get; init; }
    }
}
