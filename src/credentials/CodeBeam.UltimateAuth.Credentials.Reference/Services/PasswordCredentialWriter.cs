using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Credentials.Policies;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class PasswordCredentialWriter(
    IPasswordValidationPipeline pipeline,
    IPasswordCredentialStoreFactory credentialStores,
    IPasswordHistoryStoreFactory historyStores,
    IUAuthPasswordHasher hasher,
    EffectivePasswordPolicyOptions options)
{
    public async Task<PasswordCredential> CreateAsync(TenantKey tenant, UserKey userKey, string password, PasswordPolicyContext context,
                                                      DateTimeOffset now, CancellationToken ct)
    {
        var result = await pipeline.ValidateAsync(
            new PasswordValidationRequest
            {
                Context = context with { Password = password }
            },
            ct);

        EnsureValid(result);

        var credential = PasswordCredential.Create(
            id: null,
            tenant: tenant,
            userKey: userKey,
            secretHash: hasher.Hash(password),
            security: CredentialSecurityState.Active(),
            metadata: new CredentialMetadata(),
            now: now);

        await credentialStores.Create(tenant).AddAsync(credential, ct);

        return credential;
    }

    public async Task<PasswordCredential> ChangeAsync(PasswordCredential credential, string newPassword, PasswordPolicyContext context,
                                                      DateTimeOffset now, CancellationToken ct)
    {
        var key = new CredentialKey(credential.Tenant, credential.Id);

        var result = await pipeline.ValidateAsync(
            new PasswordValidationRequest
            {
                Context = context with
                {
                    Password = newPassword
                },
                ExistingCredentialKey = key
            },
            ct);

        EnsureValid(result);

        var oldVersion = credential.Version;
        var oldHash = credential.SecretHash;
        var newHash = hasher.Hash(newPassword);

        var updated = credential.ChangeSecret(newHash, now);

        await credentialStores.Create(credential.Tenant).SaveAsync(updated, oldVersion, ct);

        var historyStore = historyStores.Create(credential.Tenant);

        if (options.RememberLast > 0)
        {
            await historyStore.AddAsync(
                new PasswordHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    Tenant = credential.Tenant,
                    UserKey = credential.UserKey,
                    CredentialKey = key,
                    SecretHash = oldHash,
                    ReplacedAt = now
                },
                ct);
        }

        await historyStore.PruneAsync(key, options.RememberLast, ct);

        return updated;
    }

    private static void EnsureValid(PasswordPolicyResult result)
    {
        if (result.Succeeded)
            return;

        // Preserve the existing exception-based service contract.
        // The endpoint/client error mapping can be expanded later
        // to expose all violations.
        throw new UAuthValidationException(result.Violations[0].Code);
    }
}
