using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class UserLifecycleSnapshotProvider : IUserLifecycleSnapshotProvider
{
    private readonly IUserLifecycleStoreFactory _storeFactory;

    public UserLifecycleSnapshotProvider(IUserLifecycleStoreFactory storeFactory)
    {
        _storeFactory = storeFactory;
    }

    public async Task<UserLifecycleSnapshot?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var store = _storeFactory.Create(tenant);
        var profile = await store.GetAsync(new UserLifecycleKey(tenant, userKey), ct);

        if (profile is null || profile.IsDeleted)
            return null;

        return new UserLifecycleSnapshot
        {
            UserKey = profile.UserKey,
            Status = profile.Status
        };
    }
}
