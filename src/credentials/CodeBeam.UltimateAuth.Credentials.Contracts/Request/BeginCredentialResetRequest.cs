namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record BeginCredentialResetRequest
{
    public Guid Id { get; init; }
    public string? Channel { get; init; }
}
