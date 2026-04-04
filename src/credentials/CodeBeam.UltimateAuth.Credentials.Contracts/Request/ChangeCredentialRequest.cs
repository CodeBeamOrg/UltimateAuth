namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record ChangeCredentialRequest
{
    public Guid Id { get; init; }
    public string? CurrentSecret { get; init; }
    public required string NewSecret { get; init; }
}
