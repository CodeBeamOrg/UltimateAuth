using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record TokenIssuanceContext
{
    public required UserKey UserKey { get; init; }
    public required TenantKey Tenant { get; init; }
    public ClaimsSnapshot Claims { get; init; } = ClaimsSnapshot.Empty;
    public AuthSessionId? SessionId { get; init; }
    public SessionChainId? SessionChainId { get; init; }
    public RefreshChainId? RefreshChainId { get; init; }
    public DeviceId? DeviceId { get; init; }

    public string? PrimaryUserName { get; init; }
    public string? DisplayName { get; init; }
    public string? TimeZone { get; init; }
    public DateTimeOffset? AuthenticatedAt { get; init; }
}
