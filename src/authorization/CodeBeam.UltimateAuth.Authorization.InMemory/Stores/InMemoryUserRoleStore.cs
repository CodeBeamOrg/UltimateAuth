using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Authorization.InMemory;

internal sealed class InMemoryUserRoleStore : IUserRoleStore
{
    private readonly ConcurrentDictionary<(TenantKey Tenant, UserKey UserKey), HashSet<string>> _roles = new();

    public Task<IReadOnlyCollection<string>> GetRolesAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_roles.TryGetValue((tenant, userKey), out var set))
        {
            lock (set)
            {
                return Task.FromResult<IReadOnlyCollection<string>>(set.ToArray());
            }
        }

        return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
    }

    public Task AssignAsync(TenantKey tenant, UserKey userKey, string role, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var set = _roles.GetOrAdd((tenant, userKey), _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        lock (set)
        {
            set.Add(role);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(TenantKey tenant, UserKey userKey, string role, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_roles.TryGetValue((tenant, userKey), out var set))
        {
            lock (set)
            {
                set.Remove(role);
            }
        }

        return Task.CompletedTask;
    }
}
