using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Credentials.InMemory.Models;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.InMemory.Stores
{
    internal sealed class InMemoryCredentialStore<TUserId> : ICredentialStore<TUserId>, ICredentialSecretStore<TUserId>, ICredentialSecurityVersionStore<TUserId> where TUserId : notnull
    {
        private readonly ConcurrentDictionary<string, InMemoryPasswordCredential<TUserId>> _byLogin;
        private readonly ConcurrentDictionary<TUserId, List<InMemoryPasswordCredential<TUserId>>> _byUser;
        private readonly IUAuthPasswordHasher _hasher;
        private readonly IInMemoryUserIdProvider<TUserId> _userIdProvider;

        public InMemoryCredentialStore(IUAuthPasswordHasher hasher, IInMemoryUserIdProvider<TUserId> userIdProvider)
        {
            _hasher = hasher;
            _byLogin = new ConcurrentDictionary<string, InMemoryPasswordCredential<TUserId>>(StringComparer.OrdinalIgnoreCase);
            _byUser = new ConcurrentDictionary<TUserId, List<InMemoryPasswordCredential<TUserId>>>();
            _userIdProvider = userIdProvider;
            SeedDefault();
        }

        private void SeedDefault()
        {
            var credential = new InMemoryPasswordCredential<TUserId>
            {
                Login = "admin",
                UserId = _userIdProvider.GetAdminUserId(),
                IsActive = true
            };

            credential.SetPasswordHash(_hasher.Hash("admin"));

            _byLogin[credential.Login] = credential;
            _byUser[credential.UserId] = new List<InMemoryPasswordCredential<TUserId>> { credential };
        }

        public Task<ICredential<TUserId>?> FindByLoginAsync(string? tenantId, string loginIdentifier, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _byLogin.TryGetValue(loginIdentifier, out var credential);
            return Task.FromResult<ICredential<TUserId>?>(credential is { IsActive: true } ? credential : null);
        }

        public Task<IReadOnlyCollection<ICredential<TUserId>>> GetByUserAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!_byUser.TryGetValue(userId, out var list))
                return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(Array.Empty<ICredential<TUserId>>());

            return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(list.Where(c => c.IsActive).ToArray());
        }

        public Task UpdateSecretAsync(string? tenantId, TUserId userId, CredentialType type, string newSecretHash, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!_byUser.TryGetValue(userId, out var list))
                return Task.CompletedTask;

            var credential = list.FirstOrDefault(c => c.Type == type);

            if (credential is not null)
            {
                credential.SetPasswordHash(newSecretHash);
            }

            return Task.CompletedTask;
        }

        public Task<long> GetAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
        {
            if (!_byUser.TryGetValue(userId, out var list))
                return Task.FromResult(0L);

            return Task.FromResult(list.Max(c => c.SecurityVersion));
        }

        public Task IncrementAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
        {
            if (_byUser.TryGetValue(userId, out var list))
            {
                foreach (var c in list)
                    c.IncrementSecurityVersion();
            }

            return Task.CompletedTask;
        }
    }
}
