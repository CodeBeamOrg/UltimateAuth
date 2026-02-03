using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record RefreshTokenValidationContext
{
    public TenantKey Tenant { get; init; }
    public string RefreshToken { get; init; } = default!;
    public DateTimeOffset Now { get; init; }

    public required DeviceContext Device { get; init; }
    public AuthSessionId? ExpectedSessionId { get; init; }
}
