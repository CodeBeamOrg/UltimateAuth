using CodeBeam.UltimateAuth.Core;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ISecretCredential : ICredential
{
    PasswordHash SecretHash { get; }
}
