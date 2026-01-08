using CodeBeam.UltimateAuth.Core.Domain;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record AuthStateSnapshot
    {
        public string? UserId { get; init; }
        public string? TenantId { get; init; }

        public ClaimsSnapshot Claims { get; init; } = ClaimsSnapshot.Empty;

        public DateTimeOffset? AuthenticatedAt { get; init; }
    }
}
