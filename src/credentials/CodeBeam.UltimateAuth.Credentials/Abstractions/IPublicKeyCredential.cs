namespace CodeBeam.UltimateAuth.Credentials;

public interface IPublicKeyCredential : ICredential
{
    byte[] PublicKey { get; }
}
