using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal class NotSupportedSessionStoreFactory : ISessionStoreFactory
{
    public ISessionStore Create(TenantKey tenant)
    {
        throw new NotSupportedException();
    }
}
