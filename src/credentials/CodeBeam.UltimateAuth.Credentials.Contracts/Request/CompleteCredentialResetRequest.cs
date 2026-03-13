using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CompleteCredentialResetRequest
{
    public string? Identifier { get; init; }
    public CredentialType CredentialType { get; set; } = CredentialType.Password;
    public string? ResetToken { get; init; }
    public required string NewSecret { get; init; }
}
