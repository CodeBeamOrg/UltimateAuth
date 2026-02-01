using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record AuthStateSnapshot
{
    // It's not UserId type
    public string? UserId { get; init; }
    public string? TenantId { get; init; }

    public ClaimsSnapshot Claims { get; init; } = ClaimsSnapshot.Empty;

    public DateTimeOffset? AuthenticatedAt { get; init; }
}
