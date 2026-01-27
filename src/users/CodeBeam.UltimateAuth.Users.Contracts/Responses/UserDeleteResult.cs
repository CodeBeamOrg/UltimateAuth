using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserDeleteResult
{
    public required bool Succeeded { get; init; }

    public required DeleteMode Mode { get; init; }

    public string? FailureReason { get; init; }

    public static UserDeleteResult Success(DeleteMode mode)
        => new()
        {
            Succeeded = true,
            Mode = mode
        };

    public static UserDeleteResult NotFound()
        => new()
        {
            Succeeded = false,
            Mode = DeleteMode.Soft,
            FailureReason = "User not found."
        };

    public static UserDeleteResult AlreadyDeleted(DeleteMode mode)
        => new()
        {
            Succeeded = true,
            Mode = mode
        };

    public static UserDeleteResult Failed(DeleteMode mode, string reason)
        => new()
        {
            Succeeded = false,
            Mode = mode,
            FailureReason = reason
        };
}
