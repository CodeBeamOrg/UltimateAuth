using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users;

public interface IUserSecurityStateWriter
{
    Task RecordFailedLoginAsync(TenantKey tenant, UserKey userKey, DateTimeOffset at, CancellationToken ct = default);
    Task ResetFailuresAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
    Task LockUntilAsync(TenantKey tenant, UserKey userKey, DateTimeOffset lockedUntil, CancellationToken ct = default);
}
