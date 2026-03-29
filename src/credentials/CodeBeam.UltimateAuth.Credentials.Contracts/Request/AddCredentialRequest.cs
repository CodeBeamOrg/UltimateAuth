using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record AddCredentialRequest()
{
    public CredentialType Type { get; init; }
    public required string Secret { get; init; }
    public string? Source { get; init; }
}
