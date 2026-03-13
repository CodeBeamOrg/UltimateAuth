using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IUserRoleStore
{
    Task<IReadOnlyCollection<UserRole>> GetAssignmentsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
    Task AssignAsync(TenantKey tenant, UserKey userKey, RoleId roleId, DateTimeOffset assignedAt, CancellationToken ct = default);
    Task RemoveAsync(TenantKey tenant, UserKey userKey, RoleId roleId, CancellationToken ct = default);
    Task RemoveAssignmentsByRoleAsync(TenantKey tenant, RoleId roleId, CancellationToken ct = default);
    Task<int> CountAssignmentsAsync(TenantKey tenant, RoleId roleId, CancellationToken ct = default);
}
