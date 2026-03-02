namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record ChangeCredentialRequest
{
    public Guid Id { get; init; }
    public required string CurrentSecret { get; init; }
    public required string NewSecret { get; init; }
}
