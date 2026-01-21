namespace CodeBeam.UltimateAuth.Credentials
{
    public interface IPublicKeyCredential<TUserId> : ICredential<TUserId>
    {
        byte[] PublicKey { get; }
    }
}
