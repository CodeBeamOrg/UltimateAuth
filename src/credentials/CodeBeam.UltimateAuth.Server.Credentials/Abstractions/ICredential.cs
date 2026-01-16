namespace CodeBeam.UltimateAuth.Server.Credentials;

public interface ICredential<TUserId>
{
    TUserId UserId { get; }
    CredentialType Type { get; }
    bool IsActive { get; }
}
