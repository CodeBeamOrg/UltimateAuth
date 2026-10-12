using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record SessionValidationContext
{
    public TenantKey Tenant { get; init; }
    public AuthSessionId SessionId { get; init; }
    public DateTimeOffset Now { get; init; }
    public required DeviceContext Device { get; init; }

    /// <summary>
    /// Required session options for validation. Remote session validators may not include.
    /// </summary>
    public UAuthSessionOptions? SessionOptions { get; init; }
}
