namespace CodeBeam.UltimateAuth.Credentials;

public interface ISecretCredential : ICredential
{
    string SecretHash { get; }
}
