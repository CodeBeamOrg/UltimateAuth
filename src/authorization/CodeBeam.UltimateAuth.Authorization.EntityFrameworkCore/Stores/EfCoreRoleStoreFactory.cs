using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal sealed class EfCoreRoleStoreFactory<TDbContext> : IRoleStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EfCoreRoleStoreFactory(TDbContext db)
    {
        _db = db;
    }

    public IRoleStore Create(TenantKey tenant)
    {
        return new EfCoreRoleStore<TDbContext>(_db, new TenantExecutionContext(tenant));
    }
}
