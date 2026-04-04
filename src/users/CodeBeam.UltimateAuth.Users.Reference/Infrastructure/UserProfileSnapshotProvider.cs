using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class UserProfileSnapshotProvider : IUserProfileSnapshotProvider
{
    private readonly IUserProfileStoreFactory _storeFactory;

    public UserProfileSnapshotProvider(IUserProfileStoreFactory storeFactory)
    {
        _storeFactory = storeFactory;
    }

    public async Task<UserProfileSnapshot?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var store = _storeFactory.Create(tenant);
        var profile = await store.GetAsync(new UserProfileKey(tenant, userKey), ct);

        if (profile is null || profile.IsDeleted)
            return null;

        return new UserProfileSnapshot
        {
            DisplayName = profile.DisplayName,
            Language = profile.Language,
            TimeZone = profile.TimeZone
        };
    }
}
