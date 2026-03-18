using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

internal sealed class EfCoreRefreshTokenStoreFactory : IRefreshTokenStoreFactory
{
    private readonly UltimateAuthTokenDbContext _db;

    public EfCoreRefreshTokenStoreFactory(UltimateAuthTokenDbContext db)
    {
        _db = db;
    }

    public IRefreshTokenStore Create(TenantKey tenant)
    {
        return new EfCoreRefreshTokenStore(_db, new TenantContext(tenant));
    }
}
