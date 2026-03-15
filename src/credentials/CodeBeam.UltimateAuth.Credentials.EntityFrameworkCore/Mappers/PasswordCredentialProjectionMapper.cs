using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

internal static class PasswordCredentialProjectionMapper
{
    public static PasswordCredential ToDomain(this PasswordCredentialProjection p)
    {
        var security = new CredentialSecurityState(
            revokedAt: p.RevokedAt,
            expiresAt: p.ExpiresAt,
            securityStamp: p.SecurityStamp);

        var metadata = new CredentialMetadata
        {
            LastUsedAt = p.LastUsedAt,
            Source = p.Source
        };

        return PasswordCredential.Create(
            id: p.Id,
            tenant: p.Tenant,
            userKey: p.UserKey,
            secretHash: p.SecretHash,
            security: security,
            metadata: metadata,
            now: p.CreatedAt
        );
    }
    public static PasswordCredentialProjection ToProjection(this PasswordCredential c)
    {
        return new PasswordCredentialProjection
        {
            Id = c.Id,
            Tenant = c.Tenant,
            UserKey = c.UserKey,
            SecretHash = c.SecretHash,

            RevokedAt = c.Security.RevokedAt,
            ExpiresAt = c.Security.ExpiresAt,
            SecurityStamp = c.Security.SecurityStamp,

            LastUsedAt = c.Metadata.LastUsedAt,
            Source = c.Metadata.Source,

            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            DeletedAt = c.DeletedAt,
            Version = c.Version
        };
    }

    public static void UpdateProjection(this PasswordCredential c, PasswordCredentialProjection p)
    {
        p.SecretHash = c.SecretHash;

        p.RevokedAt = c.Security.RevokedAt;
        p.ExpiresAt = c.Security.ExpiresAt;
        p.SecurityStamp = c.Security.SecurityStamp;

        p.LastUsedAt = c.Metadata.LastUsedAt;
        p.Source = c.Metadata.Source;

        p.UpdatedAt = c.UpdatedAt;
    }
}