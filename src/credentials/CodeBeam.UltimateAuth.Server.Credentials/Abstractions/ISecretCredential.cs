namespace CodeBeam.UltimateAuth.Server.Credentials;

public interface ISecretCredential<TUserId> : ICredential<TUserId>
{
    string SecretHash { get; }
}
