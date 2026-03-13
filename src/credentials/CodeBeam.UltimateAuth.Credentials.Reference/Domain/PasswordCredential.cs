using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public sealed class PasswordCredential : ISecretCredential, ICredentialDescriptor, IVersionedEntity, IEntitySnapshot<PasswordCredential>, ISoftDeletable<PasswordCredential>
{
    public Guid Id { get; init; }
    public TenantKey Tenant { get; init; }
    public UserKey UserKey { get; init; }
    public CredentialType Type => CredentialType.Password;

    public string SecretHash { get; private set; } = default!;
    public CredentialSecurityState Security { get; private set; } = CredentialSecurityState.Active();
    public CredentialMetadata Metadata { get; private set; } = new CredentialMetadata();

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public long Version { get; set; }

    public bool IsRevoked => Security.RevokedAt is not null;
    public bool IsDeleted => DeletedAt is not null;
    public bool IsExpired(DateTimeOffset now) => Security.ExpiresAt is not null && Security.ExpiresAt <= now;

    private PasswordCredential() { }

    private PasswordCredential(
        Guid id,
        TenantKey tenant,
        UserKey userKey,
        string secretHash,
        CredentialSecurityState security,
        CredentialMetadata metadata,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt,
        DateTimeOffset? deletedAt,
        long version)
    {
        if (id == Guid.Empty)
            throw new UAuthValidationException("credential_id_required");

        if (string.IsNullOrWhiteSpace(secretHash))
            throw new UAuthValidationException("credential_secret_required");

        Id = id;
        Tenant = tenant;
        UserKey = userKey;
        SecretHash = secretHash;
        Security = security ?? CredentialSecurityState.Active();
        Metadata = metadata ?? new CredentialMetadata();
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
        Version = version;
    }

    public PasswordCredential Snapshot()
    {
        return new PasswordCredential
        {
            Id = Id,
            Tenant = Tenant,
            UserKey = UserKey,
            SecretHash = SecretHash,
            Security = Security,
            Metadata = Metadata,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            Version = Version
        };
    }

    public static PasswordCredential Create(
         Guid? id,
         TenantKey tenant,
         UserKey userKey,
         string secretHash,
         CredentialSecurityState security,
         CredentialMetadata metadata,
         DateTimeOffset now)
    {
        return new PasswordCredential(
            id ?? Guid.NewGuid(),
            tenant,
            userKey,
            secretHash,
            security ?? CredentialSecurityState.Active(),
            metadata ?? new CredentialMetadata(),
            now,
            null,
            null,
            0);
    }

    public PasswordCredential ChangeSecret(string newSecretHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(newSecretHash))
            throw new UAuthValidationException("credential_secret_required");

        if (IsRevoked)
            throw new UAuthConflictException("credential_revoked");

        if (IsExpired(now))
            throw new UAuthConflictException("credential_expired");

        if (string.Equals(SecretHash, newSecretHash, StringComparison.Ordinal))
            throw new UAuthValidationException("credential_secret_same");

        SecretHash = newSecretHash;
        Security = Security.RotateStamp();
        UpdatedAt = now;

        return this;
    }

    public PasswordCredential SetExpiry(DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        if (IsExpired(now))
            return this;

        Security = Security.SetExpiry(expiresAt);
        UpdatedAt = now;

        return this;
    }

    public PasswordCredential Revoke(DateTimeOffset now)
    {
        if (IsRevoked)
            return this;

        Security = Security.Revoke(now);
        UpdatedAt = now;

        return this;
    }

    public PasswordCredential MarkDeleted(DateTimeOffset now)
    {
        if (IsDeleted)
            return this;

        DeletedAt = now;
        UpdatedAt = now;

        return this;
    }
}
