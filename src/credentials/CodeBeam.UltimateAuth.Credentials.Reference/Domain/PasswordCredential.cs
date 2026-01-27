using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public sealed class PasswordCredential<TUserId> : ILoginCredential<TUserId>, ISecretCredential<TUserId>, ISecurableCredential, ICredentialDescriptor
{
    public TUserId UserId { get; }
    public CredentialType Type => CredentialType.Password;

    public string LoginIdentifier { get; }
    public string SecretHash { get; }

    public CredentialSecurityState Security { get; }
    public CredentialMetadata Metadata { get; }

    public PasswordCredential(
        TUserId userId,
        string loginIdentifier,
        string secretHash,
        CredentialSecurityState security,
        CredentialMetadata metadata)
    {
        UserId = userId;
        LoginIdentifier = loginIdentifier;
        SecretHash = secretHash;
        Security = security;
        Metadata = metadata;
    }
}
