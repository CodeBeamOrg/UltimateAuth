using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal static class PasswordCredentialFactory
{
    public static PasswordCredential Create(TenantKey tenant, UserKey userKey, string secretHash, string? source, DateTimeOffset now)
        => new PasswordCredential(
            id: Guid.NewGuid(),
            tenant: tenant,
            userKey: userKey,
            secretHash: secretHash,
            security: CredentialSecurityState.Active(),
            metadata: new CredentialMetadata { Source = source },
            createdAt: now,
            updatedAt: null);
}