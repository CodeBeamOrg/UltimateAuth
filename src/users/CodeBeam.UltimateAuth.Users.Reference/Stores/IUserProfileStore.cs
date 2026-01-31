using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserProfileStore
{
    Task<bool> ExistsAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);

    Task<UserProfile?> GetAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);

    Task<PagedResult<UserProfile>> QueryAsync(string? tenantId, UserProfileQuery query, CancellationToken ct = default);

    Task CreateAsync(string? tenantId, UserProfile profile, CancellationToken ct = default);

    Task UpdateAsync(string? tenantId, UserKey userKey, UserProfileUpdate update, DateTimeOffset updatedAt, CancellationToken ct = default);

    Task DeleteAsync(string? tenantId, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default);
}
