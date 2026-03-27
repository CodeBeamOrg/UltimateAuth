using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserIdentifierStoreFactory<TDbContext> : IUserIdentifierStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EfCoreUserIdentifierStoreFactory(TDbContext db)
    {
        _db = db;
    }

    public IUserIdentifierStore Create(TenantKey tenant)
    {
        return new EfCoreUserIdentifierStore<TDbContext>(_db, new TenantContext(tenant));
    }
}
