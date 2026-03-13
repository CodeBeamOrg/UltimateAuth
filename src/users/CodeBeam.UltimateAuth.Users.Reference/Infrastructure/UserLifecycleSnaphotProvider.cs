using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class UserLifecycleSnapshotProvider : IUserLifecycleSnapshotProvider
{
    private readonly IUserLifecycleStore _store;

    public UserLifecycleSnapshotProvider(IUserLifecycleStore store)
    {
        _store = store;
    }

    public async Task<UserLifecycleSnapshot?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var profile = await _store.GetAsync(new UserLifecycleKey(tenant, userKey), ct);

        if (profile is null || profile.IsDeleted)
            return null;

        return new UserLifecycleSnapshot
        {
            UserKey = profile.UserKey,
            Status = profile.Status
        };
    }
}
