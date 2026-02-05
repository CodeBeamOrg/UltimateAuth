using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users;

internal interface IUserSecurityStateDebugView<TUserId>
{
    IUserSecurityState? GetState(TenantKey tenant, TUserId userId);
    void Clear(TenantKey tenant, TUserId userId);
}
