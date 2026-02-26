using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

internal sealed class InMemoryPasswordCredentialState
{
    public UserKey UserKey { get; init; } = default!;
    public CredentialType Type { get; } = CredentialType.Password;

    public string SecretHash { get; set; } = default!;

    public CredentialSecurityState Security { get; set; } = default!;
    public CredentialMetadata Metadata { get; set; } = default!;
}
