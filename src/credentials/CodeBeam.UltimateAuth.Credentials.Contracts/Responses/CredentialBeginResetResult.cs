namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialBeginResetResult
{
    public DateTimeOffset ExpiresAt { get; init; }
}
