namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record ChangeCredentialRequest
{
    /// <summary>
    /// Required for self-service credential changes.
    /// Not required for authorized administrative changes.
    /// </summary>
    public string? CurrentSecret { get; init; }
    public required string NewSecret { get; init; }
}
