using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IRoleStore : IVersionedStore<Role, RoleKey>
{
    Task<Role?> GetByNameAsync(string normalizedName, CancellationToken ct = default);
    Task<IReadOnlyCollection<Role>> GetByIdsAsync(IReadOnlyCollection<RoleId> roleIds, CancellationToken ct = default);
    Task<PagedResult<Role>> QueryAsync(RoleQuery query, CancellationToken ct = default);
}
