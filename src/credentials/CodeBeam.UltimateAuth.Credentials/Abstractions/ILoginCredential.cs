namespace CodeBeam.UltimateAuth.Credentials;

public interface ILoginCredential<TUserId> : ICredential<TUserId>
{
    string LoginIdentifier { get; }
}
