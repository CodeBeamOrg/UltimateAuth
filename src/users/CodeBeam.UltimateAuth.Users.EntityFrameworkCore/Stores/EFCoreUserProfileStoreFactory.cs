using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserProfileStoreFactory<TDbContext> : IUserProfileStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EfCoreUserProfileStoreFactory(TDbContext db)
    {
        _db = db;
    }

    public IUserProfileStore Create(TenantKey tenant)
    {
        return new EfCoreUserProfileStore<TDbContext>(_db, new TenantContext(tenant));
    }
}
