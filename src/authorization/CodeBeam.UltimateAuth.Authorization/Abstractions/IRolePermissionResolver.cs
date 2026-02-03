using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IRolePermissionResolver
{
    Task<IReadOnlyCollection<Permission>> ResolveAsync(TenantKey tenant, IEnumerable<string> roles, CancellationToken ct = default);
}
