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

    private readonly ICredentialStore<UserKey> _credentials;
    private readonly IInMemoryUserIdProvider<UserKey> _ids;
    private readonly IUAuthPasswordHasher _hasher;

    public InMemoryCredentialSeedContributor(ICredentialStore<UserKey> credentials, IInMemoryUserIdProvider<UserKey> ids, IUAuthPasswordHasher hasher)
    {
        _credentials = credentials;
        _ids = ids;
        _hasher = hasher;
    }

    public async Task SeedAsync(TenantKey tenant, CancellationToken ct = default)
    {
        await SeedCredentialAsync("admin", _ids.GetAdminUserId(), tenant, ct);
        await SeedCredentialAsync("user", _ids.GetUserUserId(), tenant, ct);
    }

    private async Task SeedCredentialAsync(string login, UserKey userKey, TenantKey tenant, CancellationToken ct)
    {
        if (await _credentials.ExistsAsync(tenant, userKey, CredentialType.Password, ct))
            return;

        await _credentials.AddAsync(tenant,
            new PasswordCredential<UserKey>(
                userKey,
                login,
                _hasher.Hash(login),
                CredentialSecurityState.Active,
                new CredentialMetadata { CreatedAt = DateTimeOffset.Now}),
            ct);
    }
}
