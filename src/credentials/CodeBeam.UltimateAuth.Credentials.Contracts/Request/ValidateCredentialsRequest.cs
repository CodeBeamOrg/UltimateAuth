using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record ValidateCredentialsRequest
{
    public string Identifier { get; init; } = default!;
    public string Secret { get; init; } = default!;

    public CredentialType? CredentialType { get; init; }
}
