namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialsGetResult
{
    public IReadOnlyCollection<CredentialInfo> Credentials { get; init; } = Array.Empty<CredentialInfo>();
}
