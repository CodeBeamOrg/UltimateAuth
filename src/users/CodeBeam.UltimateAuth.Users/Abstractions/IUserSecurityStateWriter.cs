using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users;

public interface IUserSecurityStateWriter<TUserId>
{
    Task RecordFailedLoginAsync(TenantKey tenant, TUserId userId, DateTimeOffset at, CancellationToken ct = default);
    Task ResetFailuresAsync(TenantKey tenant, TUserId userId, CancellationToken ct = default);
    Task LockUntilAsync(TenantKey tenant, TUserId userId, DateTimeOffset lockedUntil, CancellationToken ct = default);
}
