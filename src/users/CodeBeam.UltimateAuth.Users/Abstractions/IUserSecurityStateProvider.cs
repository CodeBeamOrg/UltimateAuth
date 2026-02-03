using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users;

public interface IUserSecurityStateProvider<TUserId>
{
    Task<IUserSecurityState?> GetAsync(TenantKey tenant, TUserId userId, CancellationToken ct = default);
}
