using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users;

internal interface IUserSecurityStateDebugView
{
    IUserSecurityState? GetState(TenantKey tenant, UserKey userKey);
    void Clear(TenantKey tenant, UserKey userKey);
}
