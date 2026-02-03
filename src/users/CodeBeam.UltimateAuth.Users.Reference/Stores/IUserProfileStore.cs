using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserProfileStore
{
    Task<bool> ExistsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);

    Task<UserProfile?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);

    Task<PagedResult<UserProfile>> QueryAsync(TenantKey tenant, UserProfileQuery query, CancellationToken ct = default);

    Task CreateAsync(TenantKey tenant, UserProfile profile, CancellationToken ct = default);

    Task UpdateAsync(TenantKey tenant, UserKey userKey, UserProfileUpdate update, DateTimeOffset updatedAt, CancellationToken ct = default);

    Task DeleteAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default);
}
