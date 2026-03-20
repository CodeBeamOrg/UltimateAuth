using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal sealed class EfCoreUserRoleStoreFactory : IUserRoleStoreFactory
{
    private readonly UAuthAuthorizationDbContext _db;

    public EfCoreUserRoleStoreFactory(UAuthAuthorizationDbContext db)
    {
        _db = db;
    }

    public IUserRoleStore Create(TenantKey tenant)
    {
        return new EfCoreUserRoleStore(_db, new TenantContext(tenant));
    }
}
