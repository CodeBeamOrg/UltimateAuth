namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CompleteCredentialResetRequest
{
    public Guid Id { get; init; }
    public string? ResetToken { get; init; }
    public required string NewSecret { get; init; }
}
