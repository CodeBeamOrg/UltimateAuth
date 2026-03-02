using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ICredentialDescriptor
{
    Guid Id { get; }
    CredentialType Type { get; }
    CredentialSecurityState Security { get; }
    CredentialMetadata Metadata { get; }
}
