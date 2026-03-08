using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Authorization.InMemory;

internal sealed class InMemoryUserRoleStore : IUserRoleStore
{
    private readonly ConcurrentDictionary<(TenantKey, UserKey), HashSet<RoleId>> _roles = new();

    public Task<IReadOnlyCollection<RoleId>> GetRolesAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_roles.TryGetValue((tenant, userKey), out var set))
        {
            lock (set)
                return Task.FromResult<IReadOnlyCollection<RoleId>>(set.ToArray());
        }

        return Task.FromResult<IReadOnlyCollection<RoleId>>(Array.Empty<RoleId>());
    }

    public Task AssignAsync(TenantKey tenant, UserKey userKey, RoleId roleId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var set = _roles.GetOrAdd((tenant, userKey),
            _ => new HashSet<RoleId>());

        lock (set)
        {
            set.Add(roleId);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(TenantKey tenant, UserKey userKey, RoleId roleId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_roles.TryGetValue((tenant, userKey), out var set))
        {
            lock (set)
            {
                set.Remove(roleId);
            }
        }

        return Task.CompletedTask;
    }
}
