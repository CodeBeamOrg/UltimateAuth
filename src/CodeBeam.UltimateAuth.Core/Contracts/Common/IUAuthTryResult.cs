using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public interface IUAuthTryResult
{
    int? Status => null;
    string? ErrorCode => null;
    TimeSpan? RetryAfter => null;
    bool IsSuccess { get; }
    AuthFailureReason? Reason { get; }
    int? RemainingAttempts { get; }
    DateTimeOffset? LockoutUntilUtc { get; }
}
