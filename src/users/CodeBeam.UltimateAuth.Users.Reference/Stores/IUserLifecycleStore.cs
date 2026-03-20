using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserLifecycleStore : IVersionedStore<UserLifecycle, UserLifecycleKey>
{
    Task<PagedResult<UserLifecycle>> QueryAsync(UserLifecycleQuery query, CancellationToken ct = default);
}
