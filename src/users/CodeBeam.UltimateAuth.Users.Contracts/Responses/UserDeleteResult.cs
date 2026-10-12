using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserDeleteResult
{
    public required bool IsSuccess { get; init; }

    public required DeleteMode Mode { get; init; }

    public string? FailureReason { get; init; }

    public static UserDeleteResult Success(DeleteMode mode)
        => new()
        {
            IsSuccess = true,
            Mode = mode
        };

    public static UserDeleteResult NotFound()
        => new()
        {
            IsSuccess = false,
            Mode = DeleteMode.Soft,
            FailureReason = "User not found."
        };

    public static UserDeleteResult AlreadyDeleted(DeleteMode mode)
        => new()
        {
            IsSuccess = true,
            Mode = mode
        };

    public static UserDeleteResult Failed(DeleteMode mode, string reason)
        => new()
        {
            IsSuccess = false,
            Mode = mode,
            FailureReason = reason
        };
}
