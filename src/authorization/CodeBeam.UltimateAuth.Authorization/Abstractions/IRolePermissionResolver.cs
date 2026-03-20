using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IRolePermissionResolver
{
    Task<IReadOnlyCollection<Permission>> ResolveAsync(TenantKey tenant, IReadOnlyCollection<RoleId> roles, CancellationToken ct = default);
}
