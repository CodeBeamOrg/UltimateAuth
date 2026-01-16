namespace CodeBeam.UltimateAuth.Credentials;

public interface ISecretCredential<TUserId> : ICredential<TUserId>
{
    string SecretHash { get; }
}
