using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record DeleteCredentialRequest
{
    public Guid Id { get; init; }
    public DeleteMode Mode { get; init; }
}
