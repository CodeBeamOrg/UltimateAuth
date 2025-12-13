using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Core.Contexts
{
    public sealed class TokenIssueContext
    {
        public required string UserId { get; init; }
        public required string TenantId { get; init; }

        public IReadOnlyCollection<Claim> Claims { get; init; } = Array.Empty<Claim>();

        public string? SessionId { get; init; }

        public DateTimeOffset IssuedAt { get; init; } = DateTimeOffset.UtcNow;
    }
}
