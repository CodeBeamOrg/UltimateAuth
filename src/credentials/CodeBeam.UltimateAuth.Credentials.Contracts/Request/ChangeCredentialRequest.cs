namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record ChangeCredentialRequest
{
    public string? CurrentSecret { get; init; }
    public required string NewSecret { get; init; }
}
