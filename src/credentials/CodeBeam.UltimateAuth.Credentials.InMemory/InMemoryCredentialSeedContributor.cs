using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

internal sealed class InMemoryCredentialSeedContributor : ISeedContributor
{
    private static readonly Guid _adminPasswordId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _userPasswordId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public int Order => 10;

    private readonly IPasswordCredentialStore _credentials;
    private readonly IInMemoryUserIdProvider<UserKey> _ids;
    private readonly IUAuthPasswordHasher _hasher;
    private readonly IClock _clock;

    public InMemoryCredentialSeedContributor(IPasswordCredentialStore credentials, IInMemoryUserIdProvider<UserKey> ids, IUAuthPasswordHasher hasher, IClock clock)
    {
        _credentials = credentials;
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
            await _credentials.AddAsync(
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
