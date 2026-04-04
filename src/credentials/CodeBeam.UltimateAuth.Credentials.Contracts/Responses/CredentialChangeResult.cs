namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialChangeResult
{
    public bool Succeeded { get; init; }

    /// <summary>
    /// Indicates whether security version / sessions were invalidated.
    /// </summary>
    public bool SecurityInvalidated { get; init; }

    public string? FailureReason { get; init; }
}
