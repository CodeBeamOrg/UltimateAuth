using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserCreateResult
{
    public required bool Succeeded { get; init; }

    /// <summary>
    /// Created user's key (string form of UserKey).
    /// Available only when Succeeded = true.
    /// </summary>
    public string? UserKey { get; init; }

    public string? FailureReason { get; init; }

    public static UserCreateResult Success(UserKey userKey)
        => new()
        {
            Succeeded = true,
            UserKey = userKey.Value
        };

    public static UserCreateResult Failed(string reason)
        => new()
        {
            Succeeded = false,
            FailureReason = reason
        };
}

