using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserLifecycleStoreFactory : IUserLifecycleStoreFactory
{
    private readonly IDbContextFactory<UAuthUserDbContext> _dbFactory;

    public EfCoreUserLifecycleStoreFactory(IDbContextFactory<UAuthUserDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public IUserLifecycleStore Create(TenantKey tenant)
    {
        return new EfCoreUserLifecycleStore(_dbFactory, new TenantContext(tenant));
    }
}
