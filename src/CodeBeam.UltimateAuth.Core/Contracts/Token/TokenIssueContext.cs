using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record TokenIssueContext
{
    public string? TenantId { get; init; }
    public UAuthSession Session { get; init; } = default!;
    public DateTimeOffset At { get; init; }
}
