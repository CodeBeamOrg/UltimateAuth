namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record GetCredentialsResult
{
    public IReadOnlyCollection<CredentialDto> Credentials { get; init; } = Array.Empty<CredentialDto>();
}
