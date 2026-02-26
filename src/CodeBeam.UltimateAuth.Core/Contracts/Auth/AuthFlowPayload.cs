using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record AuthFlowPayload
{
    public int V { get; init; } = 1;

    public AuthFlowType Flow { get; init; }

    public string Status { get; init; } = default!;

    public AuthFailureReason? Reason { get; init; }

    public long? LockoutUntil { get; init; }

    public int? RemainingAttempts { get; init; }

    public DateTimeOffset? LockoutUntilUtc => LockoutUntil is long unix
        ? DateTimeOffset.FromUnixTimeSeconds(unix)
        : null;
}
