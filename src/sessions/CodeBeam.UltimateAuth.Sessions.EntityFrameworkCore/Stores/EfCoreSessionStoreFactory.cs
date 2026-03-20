using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal sealed class EfCoreSessionStoreFactory : ISessionStoreFactory
{
    private readonly UAuthSessionDbContext _db;

    public EfCoreSessionStoreFactory(UAuthSessionDbContext db)
    {
        _db = db;
    }

    public ISessionStore Create(TenantKey tenant)
    {
        return new EfCoreSessionStore(_db, new TenantContext(tenant));
    }
}
