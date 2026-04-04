namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record GetCredentialsResult
{
    public IReadOnlyCollection<CredentialInfo> Credentials { get; init; } = Array.Empty<CredentialInfo>();
}
