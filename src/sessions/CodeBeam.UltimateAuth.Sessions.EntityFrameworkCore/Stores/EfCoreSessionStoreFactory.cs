using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

public sealed class EfCoreSessionStoreFactory : ISessionStoreFactory
{
    private readonly IServiceProvider _sp;

    public EfCoreSessionStoreFactory(IServiceProvider sp)
    {
        _sp = sp;
    }

    public ISessionStore Create(TenantKey tenant)
    {
        return ActivatorUtilities.CreateInstance<EfCoreSessionStore>(_sp, new TenantContext(tenant));
    }

    // TODO: Implement global here
    //public ISessionStoreKernel CreateGlobal()
    //{
    //    return ActivatorUtilities.CreateInstance<EfCoreSessionStoreKernel>(_sp, new TenantContext(null, isGlobal: true));
    //}
}
