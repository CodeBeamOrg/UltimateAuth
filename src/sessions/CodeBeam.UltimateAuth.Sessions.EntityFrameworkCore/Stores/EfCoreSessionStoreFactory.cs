using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal sealed class EfCoreSessionStoreFactory<TDbContext> : ISessionStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EfCoreSessionStoreFactory(TDbContext db)
    {
        _db = db;
    }

    public ISessionStore Create(TenantKey tenant)
    {
        return new EfCoreSessionStore<TDbContext>(_db, new TenantExecutionContext(tenant));
    }
}
