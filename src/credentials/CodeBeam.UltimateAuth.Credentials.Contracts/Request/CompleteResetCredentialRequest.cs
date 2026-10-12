using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CompleteResetCredentialRequest
{
    public required string Identifier { get; init; }
    public CredentialType CredentialType { get; init; } = CredentialType.Password;
    public required string ResetToken { get; init; }
    public required string NewSecret { get; init; }
}
