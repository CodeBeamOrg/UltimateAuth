using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CompleteResetCredentialRequest
{
    public string? Identifier { get; init; }
    public CredentialType CredentialType { get; init; }
    public string? ResetToken { get; init; }
    public required string NewSecret { get; init; }
}
