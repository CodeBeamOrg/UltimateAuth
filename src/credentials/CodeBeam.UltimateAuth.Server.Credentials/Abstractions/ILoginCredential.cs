namespace CodeBeam.UltimateAuth.Server.Credentials;

public interface ILoginCredential<TUserId> : ICredential<TUserId>
{
    string LoginIdentifier { get; }
}
