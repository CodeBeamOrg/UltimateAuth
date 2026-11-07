using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record TryLoginResult : IUAuthTryResult
{
    public int? Status { get; init; }
    public string? ErrorCode { get; init; }
    public TimeSpan? RetryAfter { get; init; }
    public bool IsSuccess { get; init; }
    public AuthFailureReason? Reason { get; init; }
    public int? RemainingAttempts { get; init; }
    public DateTimeOffset? LockoutUntilUtc { get; init; }
    public bool RequiresMfa { get; init; }
    public string? PreviewReceipt { get; init; }
}
