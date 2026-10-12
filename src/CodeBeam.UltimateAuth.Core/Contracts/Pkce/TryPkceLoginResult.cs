using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record TryPkceLoginResult : IUAuthTryResult
{
    public bool IsSuccess { get; init; }
    public AuthFailureReason? Reason { get; init; }
    public int? RemainingAttempts { get; init; }
    public DateTimeOffset? LockoutUntilUtc { get; init; }
    public bool RequiresMfa { get; init; }
    public bool RetryWithNewPkce { get; init; }
}
