using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ISecurableCredential
{
    CredentialSecurityState Security { get; }
}
