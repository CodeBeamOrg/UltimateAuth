using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Authorization.InMemory;

internal sealed class InMemoryUserRoleStore : IUserRoleStore
{
    private readonly TenantKey _tenant;
    private readonly ConcurrentDictionary<UserKey, List<UserRole>> _assignments = new();

    public InMemoryUserRoleStore(TenantContext tenant)
    {
        _tenant = tenant.Tenant;
    }

    public Task<IReadOnlyCollection<UserRole>> GetAssignmentsAsync(UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_assignments.TryGetValue(userKey, out var list))
        {
            lock (list)
                return Task.FromResult<IReadOnlyCollection<UserRole>>(list.Select(x => x).ToArray());
        }

        return Task.FromResult<IReadOnlyCollection<UserRole>>(Array.Empty<UserRole>());
    }

    public Task AssignAsync(UserKey userKey, RoleId roleId, DateTimeOffset assignedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var list = _assignments.GetOrAdd(userKey, _ => new List<UserRole>());

        lock (list)
        {
            if (list.Any(x => x.RoleId == roleId))
                throw new UAuthConflictException("role_already_assigned");

            list.Add(new UserRole
            {
                Tenant = _tenant,
                UserKey = userKey,
                RoleId = roleId,
                AssignedAt = assignedAt
            });
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(UserKey userKey, RoleId roleId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_assignments.TryGetValue(userKey, out var list))
        {
            lock (list)
            {
                list.RemoveAll(x => x.RoleId == roleId);
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveAssignmentsByRoleAsync(RoleId roleId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var list in _assignments.Values)
        {
            lock (list)
            {
                list.RemoveAll(x => x.RoleId == roleId);
            }
        }

        return Task.CompletedTask;
    }

    public Task<int> CountAssignmentsAsync(RoleId roleId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var count = 0;

        foreach (var list in _assignments.Values)
        {
            lock (list)
            {
                count += list.Count(x => x.RoleId == roleId);
            }
        }

        return Task.FromResult(count);
    }
}
