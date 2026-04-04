using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal sealed class EfCoreUserRoleStoreFactory<TDbContext> : IUserRoleStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EfCoreUserRoleStoreFactory(TDbContext db)
    {
        _db = db;
    }

    public IUserRoleStore Create(TenantKey tenant)
    {
        return new EfCoreUserRoleStore<TDbContext>(_db, new TenantContext(tenant));
    }
}
