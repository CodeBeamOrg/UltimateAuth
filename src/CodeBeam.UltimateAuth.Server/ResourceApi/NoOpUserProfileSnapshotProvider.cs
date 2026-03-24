using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal class NoOpUserProfileSnapshotProvider : IUserProfileSnapshotProvider
{
    public Task<UserProfileSnapshot?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
