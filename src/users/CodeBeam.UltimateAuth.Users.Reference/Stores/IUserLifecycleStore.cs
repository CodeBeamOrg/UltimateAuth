using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserLifecycleStore : IVersionedStore<UserLifecycle, UserLifecycleKey>
{
    Task<PagedResult<UserLifecycle>> QueryAsync(TenantKey tenant, UserLifecycleQuery query, CancellationToken ct = default);
}
