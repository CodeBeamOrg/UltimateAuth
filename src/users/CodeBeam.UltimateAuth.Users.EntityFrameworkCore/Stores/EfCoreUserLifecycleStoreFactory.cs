using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserLifecycleStoreFactory : IUserLifecycleStoreFactory
{
    private readonly UAuthUserDbContext _db;

    public EfCoreUserLifecycleStoreFactory(UAuthUserDbContext db)
    {
        _db = db;
    }

    public IUserLifecycleStore Create(TenantKey tenant)
    {
        return new EfCoreUserLifecycleStore(_db, new TenantContext(tenant));
    }
}
