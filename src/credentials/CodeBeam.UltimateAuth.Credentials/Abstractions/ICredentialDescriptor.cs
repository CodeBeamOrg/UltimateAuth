using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials
{
    public interface ICredentialDescriptor
    {
        CredentialType Type { get; }
        CredentialSecurityState Security { get; }
        CredentialMetadata Metadata { get; }
    }
}
