using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public interface IUAuthTryResult
{
    bool Success { get; }
    AuthFailureReason? Reason { get; }
    int? RemainingAttempts { get; }
    DateTimeOffset? LockoutUntilUtc { get; }
}
