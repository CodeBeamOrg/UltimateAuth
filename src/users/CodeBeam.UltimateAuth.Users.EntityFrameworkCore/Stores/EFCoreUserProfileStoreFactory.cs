using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserProfileStoreFactory : IUserProfileStoreFactory
{
    private readonly UAuthUserDbContext _db;

    public EfCoreUserProfileStoreFactory(UAuthUserDbContext db)
    {
        _db = db;
    }

    public IUserProfileStore Create(TenantKey tenant)
    {
        return new EfCoreUserProfileStore(_db, new TenantContext(tenant));
    }
}
