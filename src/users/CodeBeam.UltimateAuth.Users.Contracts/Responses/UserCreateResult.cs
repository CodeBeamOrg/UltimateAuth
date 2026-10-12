using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserCreateResult
{
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Created user's key (string form of UserKey).
    /// Available only when IsSuccess = true.
    /// </summary>
    public string? UserKey { get; init; }

    public string? FailureReason { get; init; }

    public static UserCreateResult Success(UserKey userKey)
        => new()
        {
            IsSuccess = true,
            UserKey = userKey.Value
        };

    public static UserCreateResult Failed(string reason)
        => new()
        {
            IsSuccess = false,
            FailureReason = reason
        };
}
