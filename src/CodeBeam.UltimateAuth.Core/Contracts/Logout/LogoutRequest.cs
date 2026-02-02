using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record LogoutRequest
{
    public TenantKey Tenant { get; init; }
    public AuthSessionId SessionId { get; init; }
    public DateTimeOffset? At { get; init; }
}
