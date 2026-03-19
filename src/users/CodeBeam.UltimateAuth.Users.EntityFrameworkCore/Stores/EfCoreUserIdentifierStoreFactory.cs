using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserIdentifierStoreFactory : IUserIdentifierStoreFactory
{
    private readonly UAuthUserDbContext _db;

    public EfCoreUserIdentifierStoreFactory(UAuthUserDbContext db)
    {
        _db = db;
    }

    public IUserIdentifierStore Create(TenantKey tenant)
    {
        return new EfCoreUserIdentifierStore(_db, new TenantContext(tenant));
    }
}
