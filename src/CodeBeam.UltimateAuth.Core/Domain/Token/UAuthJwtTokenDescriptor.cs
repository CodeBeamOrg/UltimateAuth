using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Domain;

/// <summary>
/// Framework-agnostic JWT description used by IJwtTokenGenerator.
/// </summary>
public sealed class UAuthJwtTokenDescriptor
{
    public required string Subject { get; init; }

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required DateTimeOffset IssuedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public TenantKey Tenant { get; init; }

    public IReadOnlyDictionary<string, object>? Claims { get; init; }

    public string? KeyId { get; init; } // kid
}
