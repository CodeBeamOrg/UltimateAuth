namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record BeginCredentialResetRequest
{
    public string? Reason { get; init; }
}
