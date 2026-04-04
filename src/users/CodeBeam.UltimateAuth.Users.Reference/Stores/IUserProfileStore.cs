using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserProfileStore : IVersionedStore<UserProfile, UserProfileKey>
{
    Task<PagedResult<UserProfile>> QueryAsync(UserProfileQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<UserProfile>> GetByUsersAsync(IReadOnlyList<UserKey> userKeys, CancellationToken ct = default);
}
