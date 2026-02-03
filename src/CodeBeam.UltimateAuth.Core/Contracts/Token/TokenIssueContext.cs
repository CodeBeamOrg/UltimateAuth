using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record TokenIssueContext
{
    public TenantKey Tenant { get; init; }
    public UAuthSession Session { get; init; } = default!;
    public DateTimeOffset At { get; init; }
}
