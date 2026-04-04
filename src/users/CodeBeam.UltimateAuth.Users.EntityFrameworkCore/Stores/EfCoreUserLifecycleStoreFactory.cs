using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserLifecycleStoreFactory<TDbContext> : IUserLifecycleStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EfCoreUserLifecycleStoreFactory(TDbContext db)
    {
        _db = db;
    }

    public IUserLifecycleStore Create(TenantKey tenant)
    {
        return new EfCoreUserLifecycleStore<TDbContext>(_db, new TenantContext(tenant));
    }
}
