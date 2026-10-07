namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record BeginCredentialResetResult
{
    public DateTimeOffset ExpiresAt { get; init; }
}
