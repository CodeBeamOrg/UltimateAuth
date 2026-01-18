namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record ChangeCredentialResult
{
    public CredentialType Type { get; init; }

    public required bool Succeeded { get; init; }

    /// <summary>
    /// Indicates whether existing sessions / tokens were invalidated.
    /// </summary>
    public bool SecurityInvalidated { get; init; }

    public string? FailureReason { get; init; }

    public static ChangeCredentialResult Success(CredentialType type)
        => new()
        {
            Succeeded = true,
            Type = type,
            SecurityInvalidated = false
        };

    public static ChangeCredentialResult Failed(string reason)
        => new()
        {
            Succeeded = false,
            FailureReason = reason
        };
}
