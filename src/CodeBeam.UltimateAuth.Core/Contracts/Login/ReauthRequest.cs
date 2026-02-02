using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record ReauthRequest
{
    public TenantKey Tenant { get; init; }
    public AuthSessionId SessionId { get; init; }
    public string Secret { get; init; } = default!;
}
