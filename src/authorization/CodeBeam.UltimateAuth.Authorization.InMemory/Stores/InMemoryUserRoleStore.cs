using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Authorization.InMemory
{
    internal sealed class InMemoryUserRoleStore : IUserRoleStore
    {
        private readonly ConcurrentDictionary<UserKey, HashSet<string>> _roles = new();

        public InMemoryUserRoleStore(IInMemoryUserIdProvider<UserKey> ids)
        {
            var key = ids.GetAdminUserId();
            _roles[key] = new HashSet<string>
            {
                "Admin"
            };
        }

        public Task<IReadOnlyCollection<string>> GetRolesAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_roles.TryGetValue(userKey, out var set))
                return Task.FromResult<IReadOnlyCollection<string>>(set.ToArray());

            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        public Task AssignAsync(string? tenantId, UserKey userKey, string role, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var set = _roles.GetOrAdd(userKey, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            set.Add(role);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string? tenantId, UserKey userKey, string role, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_roles.TryGetValue(userKey, out var set))
                set.Remove(role);

            return Task.CompletedTask;
        }
    }

}
