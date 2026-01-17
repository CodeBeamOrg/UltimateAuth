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
}
