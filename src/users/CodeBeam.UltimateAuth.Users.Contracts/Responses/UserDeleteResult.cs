namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserDeleteResult
{
    public required bool Succeeded { get; init; }

    public required UserDeleteMode Mode { get; init; }

    public string? FailureReason { get; init; }

    public static UserDeleteResult Success(UserDeleteMode mode)
        => new()
        {
            Succeeded = true,
            Mode = mode
        };

    public static UserDeleteResult NotFound()
        => new()
        {
            Succeeded = false,
            Mode = UserDeleteMode.Soft,
            FailureReason = "User not found."
        };

    public static UserDeleteResult AlreadyDeleted(UserDeleteMode mode)
        => new()
        {
            Succeeded = true,
            Mode = mode
        };

    public static UserDeleteResult Failed(UserDeleteMode mode, string reason)
        => new()
        {
            Succeeded = false,
            Mode = mode,
            FailureReason = reason
        };
}
