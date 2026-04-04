using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users;

public interface IUserLifecycleSnapshotProvider
{
    Task<UserLifecycleSnapshot?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
}
