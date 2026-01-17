using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ICredential<TUserId>
{
    TUserId UserId { get; }
    CredentialType Type { get; }
    bool IsActive { get; }
}
