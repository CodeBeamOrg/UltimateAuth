using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record AuthContext
{
    public UAuthClientProfile ClientProfile { get; set; }

    public TenantKey Tenant { get; init; }

    public AuthOperation Operation { get; init; }

    public UAuthMode Mode { get; init; }

    public SessionSecurityContext? Session { get; init; }

    public required DeviceContext Device { get; init; }

    public DateTimeOffset At { get; init; }
}
