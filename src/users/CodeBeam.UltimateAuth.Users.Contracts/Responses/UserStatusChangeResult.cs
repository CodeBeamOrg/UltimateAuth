using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserStatusChangeResult
{
    public required bool IsSuccess { get; init; }

    public UserStatus? PreviousStatus { get; init; }

    public UserStatus? CurrentStatus { get; init; }

    public string? FailureReason { get; init; }

    public static UserStatusChangeResult Success(UserStatus previous, UserStatus current)
        => new()
        {
            IsSuccess = true,
            PreviousStatus = previous,
            CurrentStatus = current
        };

    public static UserStatusChangeResult NoChange(UserStatus status)
        => new()
        {
            IsSuccess = true,
            PreviousStatus = status,
            CurrentStatus = status
        };

    public static UserStatusChangeResult NotFound()
        => new()
        {
            IsSuccess = false,
            FailureReason = "User not found."
        };

    public static UserStatusChangeResult Failed(string reason)
        => new()
        {
            IsSuccess = false,
            FailureReason = reason
        };
}
