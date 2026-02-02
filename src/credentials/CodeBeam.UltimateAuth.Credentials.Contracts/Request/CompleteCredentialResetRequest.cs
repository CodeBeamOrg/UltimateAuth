namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CompleteCredentialResetRequest
{
    public required string NewSecret { get; init; }
    public string? Source { get; init; }
}
