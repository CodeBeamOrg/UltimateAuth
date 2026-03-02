using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public sealed class PasswordCredential : ISecretCredential, ICredentialDescriptor, IVersionedEntity
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

    public long Version { get; private set; }

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
        Version = 0;
    }

    public static PasswordCredential Create(
        Guid? id,
        TenantKey tenant,
        UserKey userKey,
        string secretHash,
        CredentialSecurityState security,
        CredentialMetadata metadata,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
    {
        return new(
            id ?? Guid.NewGuid(),
            tenant,
            userKey,
            secretHash,
            security,
            metadata,
            createdAt,
            updatedAt);
    }

    public void ChangeSecret(string newSecretHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(newSecretHash))
            throw new UAuthValidationException("credential_secret_required");

        if (IsRevoked)
            throw new UAuthConflictException("credential_revoked");

        if (IsExpired(now))
            throw new UAuthConflictException("credential_expired");

        if (SecretHash == newSecretHash)
            throw new UAuthValidationException("credential_secret_same");

        SecretHash = newSecretHash;
        Security = Security.RotateStamp();
        UpdatedAt = now;
        Version++;
    }

    public void SetExpiry(DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        Security = Security.SetExpiry(expiresAt);
        UpdatedAt = now;
        Version++;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (IsRevoked)
            return;

        Security = Security.Revoke(now);
        UpdatedAt = now;
        Version++;
    }

    public void RegisterFailedAttempt(DateTimeOffset now, int threshold, TimeSpan duration)
    {
        Security = Security.RegisterFailedAttempt(now, threshold, duration);
        UpdatedAt = now;
        Version++;
    }

    public void RegisterSuccessfulAuthentication(DateTimeOffset now)
    {
        Security = Security.RegisterSuccessfulAuthentication();
        UpdatedAt = now;
        Version++;
    }

    public void BeginReset(DateTimeOffset now, TimeSpan validity)
    {
        Security = Security.BeginReset(now, validity);
        UpdatedAt = now;
        Version++;
    }

    public void CompleteReset(DateTimeOffset now)
    {
        Security = Security.CompleteReset(now);

        UpdatedAt = now;
        Version++;
    }
}
