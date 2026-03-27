using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Sample.Seed;

internal sealed class CredentialSeedContributor : ISeedContributor
{
    private static readonly Guid _adminPasswordId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _userPasswordId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public int Order => 10;

    private readonly IPasswordCredentialStoreFactory _credentialFactory;
    private readonly IUserIdProvider<UserKey> _ids;
    private readonly IUAuthPasswordHasher _hasher;
    private readonly IClock _clock;

    public CredentialSeedContributor(IPasswordCredentialStoreFactory credentialFactory, IUserIdProvider<UserKey> ids, IUAuthPasswordHasher hasher, IClock clock)
    {
        _credentialFactory = credentialFactory;
        _ids = ids;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task SeedAsync(TenantKey tenant, CancellationToken ct = default)
    {
        await SeedCredentialAsync(_ids.GetAdminUserId(), _adminPasswordId, "admin", tenant, ct);
        await SeedCredentialAsync(_ids.GetUserUserId(), _userPasswordId, "user", tenant, ct);
    }

    private async Task SeedCredentialAsync(UserKey userKey, Guid credentialId, string secretHash, TenantKey tenant, CancellationToken ct)
    {
        try
        {
            var credentialStore = _credentialFactory.Create(tenant);

            var existing = await credentialStore.GetByUserAsync(userKey, ct);

            if (existing.Any(x => x.Id == credentialId))
                return;

            await credentialStore.AddAsync(
                PasswordCredential.Create(
                    credentialId,
                    tenant,
                    userKey,
                    _hasher.Hash(secretHash),
                    CredentialSecurityState.Active(),
                    new CredentialMetadata(),
                    _clock.UtcNow),
                ct);
        }
        catch (UAuthConflictException)
        {
            // already seeded
        }
    }
}
