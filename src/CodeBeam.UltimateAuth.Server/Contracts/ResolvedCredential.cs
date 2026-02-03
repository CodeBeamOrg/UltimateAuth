using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Server.Contracts;

public sealed record ResolvedCredential
{
    public PrimaryCredentialKind Kind { get; init; }

    /// <summary>
    /// Raw credential value (session id / jwt / opaque)
    /// </summary>
    public string Value { get; init; } = default!;

    public TenantKey Tenant { get; init; }

    public DeviceInfo Device { get; init; } = default!;
}
