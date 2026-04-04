using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserStatusChangeResult
{
    public required bool Succeeded { get; init; }

    public UserStatus? PreviousStatus { get; init; }

    public UserStatus? CurrentStatus { get; init; }

    public string? FailureReason { get; init; }

    public static UserStatusChangeResult Success(UserStatus previous, UserStatus current)
        => new()
        {
            Succeeded = true,
            PreviousStatus = previous,
            CurrentStatus = current
        };

    public static UserStatusChangeResult NoChange(UserStatus status)
        => new()
        {
            Succeeded = true,
            PreviousStatus = status,
            CurrentStatus = status
        };

    public static UserStatusChangeResult NotFound()
        => new()
        {
            Succeeded = false,
            FailureReason = "User not found."
        };

    public static UserStatusChangeResult Failed(string reason)
        => new()
        {
            Succeeded = false,
            FailureReason = reason
        };
}
