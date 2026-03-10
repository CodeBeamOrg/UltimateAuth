using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IRoleStore : IVersionedStore<Role, RoleKey>
{
    Task<Role?> GetByNameAsync(TenantKey tenant, string normalizedName, CancellationToken ct = default);
    Task<IReadOnlyCollection<Role>> GetByIdsAsync(TenantKey tenant, IReadOnlyCollection<RoleId> roleIds, CancellationToken ct = default);
    Task<PagedResult<Role>> QueryAsync(TenantKey tenant, RoleQuery query, CancellationToken ct = default);
}
