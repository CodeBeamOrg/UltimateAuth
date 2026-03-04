namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record BeginCredentialResetResult
{
    public string? Token { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}
