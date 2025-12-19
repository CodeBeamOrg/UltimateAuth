using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.InMemory
{
    internal sealed class InMemoryUserStore : IUAuthUserStore<UserId>
    {
        private readonly ConcurrentDictionary<string, InMemoryCredentialUser> _usersByUsername;
        private readonly ConcurrentDictionary<UserId, InMemoryCredentialUser> _usersById;

        public InMemoryUserStore(IEnumerable<InMemoryCredentialUser> seededUsers)
        {
            _usersByUsername = new ConcurrentDictionary<string, InMemoryCredentialUser>(
                StringComparer.OrdinalIgnoreCase);

            _usersById = new ConcurrentDictionary<UserId, InMemoryCredentialUser>();

            foreach (var user in seededUsers)
            {
                _usersByUsername[user.Username] = user;
                _usersById[user.UserId] = user;
            }
        }

        public Task<IUser<UserId>?> FindByIdAsync(
            string? tenantId,
            UserId userId,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            _usersById.TryGetValue(userId, out var user);
            return Task.FromResult<IUser<UserId>?>(user is { IsActive: true } ? user : null);
        }

        public Task<UserRecord<UserId>?> FindByUsernameAsync(
            string? tenantId,
            string username,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!_usersByUsername.TryGetValue(username, out var user) || user.IsActive is false)
                return Task.FromResult<UserRecord<UserId>?>(null);

            // Core’daki UserRecord’u kullanıyorsun; InMemory tarafı buna map eder.
            var record = new UserRecord<UserId>
            {
                Id = user.UserId,
                Username = user.Username,
                PasswordHash = user.PasswordHash,
                // ClaimsSnapshot varsa burada Empty bırakılabilir.
                // Claims = ClaimsSnapshot.Empty,
                RequiresMfa = false,
                IsActive = user.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false
            };

            return Task.FromResult<UserRecord<UserId>?>(record);
        }

        public Task<IUser<UserId>?> FindByLoginAsync(
            string? tenantId,
            string login,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            _usersByUsername.TryGetValue(login, out var user);
            return Task.FromResult<IUser<UserId>?>(user is { IsActive: true } ? user : null);
        }

        public Task<string?> GetPasswordHashAsync(
            string? tenantId,
            UserId userId,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            return Task.FromResult(
                _usersById.TryGetValue(userId, out var user)
                    ? user.PasswordHash
                    : null);
        }

        public Task SetPasswordHashAsync(
            string? tenantId,
            UserId userId,
            string passwordHash,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            if (_usersById.TryGetValue(userId, out var user))
            {
                user.SetPasswordHash(passwordHash);
            }

            return Task.CompletedTask;
        }

        public Task<long> GetSecurityVersionAsync(string? tenantId, UserId userId, CancellationToken token = default)
        {
            return Task.FromResult(
                _usersById.TryGetValue(userId, out var user)
                    ? user.SecurityVersion
                    : 0L);
        }

        public Task IncrementSecurityVersionAsync(string? tenantId, UserId userId, CancellationToken token = default)
        {
            if (_usersById.TryGetValue(userId, out var user))
            {
                user.IncrementSecurityVersion();
            }

            return Task.CompletedTask;
        }
    }
}
