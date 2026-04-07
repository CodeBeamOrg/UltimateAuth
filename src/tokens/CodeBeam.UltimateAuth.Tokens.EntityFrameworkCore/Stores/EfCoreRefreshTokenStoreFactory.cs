using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

internal sealed class EfCoreRefreshTokenStoreFactory<TDbContext> : IRefreshTokenStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EfCoreRefreshTokenStoreFactory(TDbContext db)
    {
        _db = db;
    }

    public IRefreshTokenStore Create(TenantKey tenant)
    {
        return new EfCoreRefreshTokenStore<TDbContext>(_db, new TenantExecutionContext(tenant));
    }
}
