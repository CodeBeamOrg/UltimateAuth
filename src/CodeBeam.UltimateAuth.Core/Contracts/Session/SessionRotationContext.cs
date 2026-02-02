using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record SessionRotationContext
{
    public TenantKey Tenant { get; init; }
    public AuthSessionId CurrentSessionId { get; init; }
    public UserKey UserKey { get; init; }
    public DateTimeOffset Now { get; init; }
    public required DeviceContext Device { get; init; }
    public ClaimsSnapshot? Claims { get; init; }
    public required SessionMetadata Metadata { get; init; } = SessionMetadata.Empty;
}
