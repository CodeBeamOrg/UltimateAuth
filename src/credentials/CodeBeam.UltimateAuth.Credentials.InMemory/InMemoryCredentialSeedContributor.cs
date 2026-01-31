using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Credentials.InMemory
{
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

        public async Task SeedAsync(string? tenantId, CancellationToken ct = default)
        {
            await SeedCredentialAsync("admin", _ids.GetAdminUserId(), tenantId, ct);
            await SeedCredentialAsync("user", _ids.GetUserUserId(), tenantId, ct);
        }

        private async Task SeedCredentialAsync(string login, UserKey userKey, string? tenantId, CancellationToken ct)
        {
            if (await _credentials.ExistsAsync(tenantId, userKey, CredentialType.Password, ct))
                return;

            await _credentials.AddAsync(tenantId,
                new PasswordCredential<UserKey>(
                    userKey,
                    login,
                    _hasher.Hash(login),
                    CredentialSecurityState.Active,
                    new CredentialMetadata(DateTimeOffset.Now, null, null)),
                ct);
        }
    }
}
