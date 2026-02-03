using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

internal sealed class InMemoryPasswordCredentialState<TUserId>
{
    public TUserId UserId { get; init; } = default!;
    public CredentialType Type { get; } = CredentialType.Password;

    public string Login { get; init; } = default!;
    public string SecretHash { get; set; } = default!;

    public CredentialSecurityState Security { get; set; } = default!;
    public CredentialMetadata Metadata { get; set; } = default!;
}
