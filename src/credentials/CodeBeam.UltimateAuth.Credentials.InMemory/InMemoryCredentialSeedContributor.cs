using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

internal sealed class InMemoryCredentialSeedContributor : ISeedContributor
{
    public int Order => 10;

    private readonly ICredentialStore _credentials;
    private readonly IInMemoryUserIdProvider<UserKey> _ids;
    private readonly IUAuthPasswordHasher _hasher;

    public InMemoryCredentialSeedContributor(ICredentialStore credentials, IInMemoryUserIdProvider<UserKey> ids, IUAuthPasswordHasher hasher)
    {
        _credentials = credentials;
        _ids = ids;
        _hasher = hasher;
    }

    public async Task SeedAsync(TenantKey tenant, CancellationToken ct = default)
    {
        await SeedCredentialAsync(_ids.GetAdminUserId(), "admin", tenant, ct);
        await SeedCredentialAsync(_ids.GetUserUserId(), "user", tenant, ct);
    }

    private async Task SeedCredentialAsync(UserKey userKey, string hash, TenantKey tenant, CancellationToken ct)
    {
        if (await _credentials.ExistsAsync(tenant, userKey, CredentialType.Password, null, ct))
            return;

        await _credentials.AddAsync(tenant,
            new PasswordCredential(
                Guid.NewGuid(),
                tenant,
                userKey,
                _hasher.Hash(hash),
                CredentialSecurityState.Active(),
                new CredentialMetadata(),
                DateTimeOffset.UtcNow,
                null),
            ct);
    }
}
