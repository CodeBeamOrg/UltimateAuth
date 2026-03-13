using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Authorization.InMemory;

internal sealed class InMemoryUserRoleStore : IUserRoleStore
{
    private readonly ConcurrentDictionary<(TenantKey, UserKey), List<UserRole>> _assignments = new();

    public Task<IReadOnlyCollection<UserRole>> GetAssignmentsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_assignments.TryGetValue((tenant, userKey), out var list))
        {
            lock (list)
                return Task.FromResult<IReadOnlyCollection<UserRole>>(list.ToArray());
        }

        return Task.FromResult<IReadOnlyCollection<UserRole>>(Array.Empty<UserRole>());
    }

    public Task AssignAsync(TenantKey tenant, UserKey userKey, RoleId roleId, DateTimeOffset assignedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var list = _assignments.GetOrAdd((tenant, userKey), _ => new List<UserRole>());

        lock (list)
        {
            if (list.Any(x => x.RoleId == roleId))
                throw new UAuthConflictException("Role is already assigned to the user.");

            list.Add(new UserRole
            {
                Tenant = tenant,
                UserKey = userKey,
                RoleId = roleId,
                AssignedAt = assignedAt
            });
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(TenantKey tenant, UserKey userKey, RoleId roleId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_assignments.TryGetValue((tenant, userKey), out var list))
        {
            lock (list)
            {
                list.RemoveAll(x => x.RoleId == roleId);
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveAssignmentsByRoleAsync(TenantKey tenant, RoleId roleId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var kv in _assignments)
        {
            if (kv.Key.Item1 != tenant)
                continue;

            var list = kv.Value;

            lock (list)
            {
                list.RemoveAll(x => x.RoleId == roleId);
            }
        }

        return Task.CompletedTask;
    }

    public Task<int> CountAssignmentsAsync(TenantKey tenant, RoleId roleId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var count = 0;

        foreach (var kv in _assignments)
        {
            if (kv.Key.Item1 != tenant)
                continue;

            var list = kv.Value;

            lock (list)
            {
                count += list.Count(x => x.RoleId == roleId);
            }
        }

        return Task.FromResult(count);
    }
}
