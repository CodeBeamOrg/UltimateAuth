namespace CodeBeam.UltimateAuth.Server.Credentials;

public sealed class PasswordCredential<TUserId> : ILoginCredential<TUserId>, ISecretCredential<TUserId>
{
    public TUserId UserId { get; }
    public CredentialType Type => CredentialType.Password;
    public CredentialStatus Status { get; }
    public string LoginIdentifier { get; }
    public string SecretHash { get; }
    public CredentialMetadata Metadata { get; }

    public bool IsActive => Status == CredentialStatus.Active;

    public PasswordCredential(
        TUserId userId,
        string loginIdentifier,
        string secretHash,
        CredentialStatus status,
        CredentialMetadata metadata)
    {
        UserId = userId;
        LoginIdentifier = loginIdentifier;
        SecretHash = secretHash;
        Status = status;
        Metadata = metadata;
    }
}
