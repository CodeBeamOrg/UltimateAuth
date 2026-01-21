namespace CodeBeam.UltimateAuth.Credentials.Contracts
{
    public sealed record GetCredentialsResult(
        IReadOnlyCollection<CredentialDto> Credentials);
}
