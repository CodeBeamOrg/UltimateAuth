using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public sealed class PasswordCredential : ISecretCredential, ICredentialDescriptor
{
    public Guid Id { get; init; }
    public TenantKey Tenant { get; init; }
    public UserKey UserKey { get; init; }
    public CredentialType Type => CredentialType.Password;

    public string SecretHash { get; private set; }

    public CredentialSecurityState Security { get; private set; }
    public CredentialMetadata Metadata { get; private set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; private set; }


    public bool IsRevoked => Security.RevokedAt is not null;
    public bool IsExpired(DateTimeOffset now) => Security.ExpiresAt is not null && Security.ExpiresAt <= now;

    public PasswordCredential(
        Guid? id,
        TenantKey tenant,
        UserKey userKey,
        string secretHash,
        CredentialSecurityState security,
        CredentialMetadata metadata,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
    {
        Id = id ?? Guid.NewGuid();
        Tenant = tenant;
        UserKey = userKey;
        SecretHash = secretHash;
        Security = security;
        Metadata = metadata;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public void ChangeSecret(string newSecretHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(newSecretHash))
            throw new ArgumentException("Secret hash cannot be empty.", nameof(newSecretHash));

        if (IsRevoked)
            throw new InvalidOperationException("Cannot change secret of a revoked credential.");

        SecretHash = newSecretHash;
        UpdatedAt = now;
        Security = Security.RotateStamp();
    }

    public void SetExpiry(DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        Security = Security.SetExpiry(expiresAt);
        UpdatedAt = now;
    }

    public void UpdateSecurity(CredentialSecurityState security, DateTimeOffset now)
    {
        Security = security;
        UpdatedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (IsRevoked)
            return;
        Security = Security.Revoke(now);
        UpdatedAt = now;
    }
}
