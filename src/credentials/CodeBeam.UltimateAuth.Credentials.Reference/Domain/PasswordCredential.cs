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

    public string SecretHash { get; init; } = default!;
    public CredentialSecurityState Security { get; init; } = CredentialSecurityState.Active();
    public CredentialMetadata Metadata { get; init; } = new CredentialMetadata();

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }

    public long Version { get; private set; }

    public bool IsRevoked => Security.RevokedAt is not null;
    public bool IsExpired(DateTimeOffset now) => Security.ExpiresAt is not null && Security.ExpiresAt <= now;

    public PasswordCredential() { }

    private PasswordCredential(
        Guid id,
        TenantKey tenant,
        UserKey userKey,
        string secretHash,
        CredentialSecurityState security,
        CredentialMetadata metadata,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt,
        long version)
    {
        if (id == Guid.Empty) throw new UAuthValidationException("credential_id_required");
        if (string.IsNullOrWhiteSpace(secretHash)) throw new UAuthValidationException("credential_secret_required");

        Id = id;
        Tenant = tenant;
        UserKey = userKey;
        SecretHash = !string.IsNullOrWhiteSpace(secretHash)
            ? secretHash
            : throw new UAuthValidationException("credential_secret_required");
        Security = security;
        Metadata = metadata ?? new CredentialMetadata();
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Version = version;
    }

    public static PasswordCredential Create(
        Guid? id,
        TenantKey tenant,
        UserKey userKey,
        string secretHash,
        CredentialSecurityState security,
        CredentialMetadata metadata,
        DateTimeOffset now)
        => new(
            id: id ?? Guid.NewGuid(),
            tenant: tenant,
            userKey: userKey,
            secretHash: secretHash,
            security: security,
            metadata: metadata,
            createdAt: now,
            updatedAt: null,
            version: 0);

    private PasswordCredential Next(
        string? secretHash = null,
        CredentialSecurityState? security = null,
        CredentialMetadata? metadata = null,
        DateTimeOffset? updatedAt = null)
        => new(
            id: Id,
            tenant: Tenant,
            userKey: UserKey,
            secretHash: secretHash ?? SecretHash,
            security: security ?? Security,
            metadata: metadata ?? Metadata,
            createdAt: CreatedAt,
            updatedAt: updatedAt ?? UpdatedAt,
            version: Version + 1);

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

        return Next(newSecretHash, Security.RotateStamp(), updatedAt: now);
    }

    public PasswordCredential SetExpiry(DateTimeOffset? expiresAt, DateTimeOffset now)
        => Next(security: Security.SetExpiry(expiresAt), updatedAt: now);

    public PasswordCredential Revoke(DateTimeOffset now)
    {
        if (IsRevoked)
            return this;

        return Next(security: Security.Revoke(now), updatedAt: now);
    }
}
