using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserProfileStore : IVersionedStore<UserProfile, UserProfileKey>
{
    Task<PagedResult<UserProfile>> QueryAsync(TenantKey tenant, UserProfileQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<UserProfile>> GetByUsersAsync(TenantKey tenant, IReadOnlyList<UserKey> userKeys, CancellationToken ct = default);
}
