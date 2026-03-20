using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal sealed class EfCoreRoleStoreFactory : IRoleStoreFactory
{
    private readonly UAuthAuthorizationDbContext _db;

    public EfCoreRoleStoreFactory(UAuthAuthorizationDbContext db)
    {
        _db = db;
    }

    public IRoleStore Create(TenantKey tenant)
    {
        return new EfCoreRoleStore(_db, new TenantContext(tenant));
    }
}
