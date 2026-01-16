using CodeBeam.UltimateAuth.Core.Infrastructure;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Users.InMemory
{
    internal sealed class InMemoryUserStore<TUserId> : IUserStore<TUserId> where TUserId : notnull
    {
        private readonly ConcurrentDictionary<TUserId, InMemoryUser<TUserId>> _usersById;
        private readonly ConcurrentDictionary<string, TUserId> _userIdsByLogin;

        public InMemoryUserStore(IEnumerable<InMemoryUser<TUserId>>? seededUsers = null)
        {
            _usersById = new ConcurrentDictionary<TUserId, InMemoryUser<TUserId>>();
            _userIdsByLogin = new ConcurrentDictionary<string, TUserId>(StringComparer.OrdinalIgnoreCase);

            if (seededUsers is null)
                return;

            foreach (var user in seededUsers)
            {
                _usersById[user.Id] = user;
                _userIdsByLogin[user.Login] = user.Id;
            }
        }

        public Task<UserRecord<TUserId>?> FindByIdAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!_usersById.TryGetValue(userId, out var user))
                return Task.FromResult<UserRecord<TUserId>?>(null);

            if (user.IsDeleted)
                return Task.FromResult<UserRecord<TUserId>?>(null);

            return Task.FromResult<UserRecord<TUserId>?>(Map(user));
        }

        public Task<UserRecord<TUserId>?> FindByLoginAsync(string? tenantId, string login, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!_userIdsByLogin.TryGetValue(login, out var userId))
                return Task.FromResult<UserRecord<TUserId>?>(null);

            if (!_usersById.TryGetValue(userId, out var user))
                return Task.FromResult<UserRecord<TUserId>?>(null);

            if (user.IsDeleted)
                return Task.FromResult<UserRecord<TUserId>?>(null);

            return Task.FromResult<UserRecord<TUserId>?>(Map(user));
        }

        public Task<bool> ExistsAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            return Task.FromResult(
                _usersById.TryGetValue(userId, out var user) && !user.IsDeleted);
        }

        private static UserRecord<TUserId> Map(InMemoryUser<TUserId> user)
        {
            return new UserRecord<TUserId>
            {
                Id = user.Id,
                Login = user.Login,
                IsActive = user.IsActive,
                IsDeleted = user.IsDeleted,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
