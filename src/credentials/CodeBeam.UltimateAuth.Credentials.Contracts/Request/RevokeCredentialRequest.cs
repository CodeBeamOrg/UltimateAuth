namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record RevokeCredentialRequest
{
    /// <summary>
    /// If specified, credential is revoked until this time.
    /// Null means permanent revocation.
    /// </summary>
    public DateTimeOffset? Until { get; init; }

    /// <summary>
    /// Optional human-readable reason for audit/logging purposes.
    /// </summary>
    public string? Reason { get; init; }
}
