using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class UserProfileSnapshotProvider : IUserProfileSnapshotProvider
{
    private readonly IUserProfileStore _store;

    public UserProfileSnapshotProvider(IUserProfileStore store)
    {
        _store = store;
    }

    public async Task<UserProfileSnapshot?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var profile = await _store.GetAsync(tenant, userKey, ct);

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
