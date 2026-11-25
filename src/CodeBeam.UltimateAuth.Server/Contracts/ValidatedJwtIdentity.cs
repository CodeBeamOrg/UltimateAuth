using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.Contracts;

/// <summary>
/// Identity established by successful cryptographic JWT validation.
/// This result does not imply that live application policies passed.
/// </summary>
public sealed record ValidatedJwtIdentity
{
    public required string Subject { get; init; }

    public required string TenantId { get; init; }

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required IReadOnlyCollection<Claim> Claims { get; init; }
}
