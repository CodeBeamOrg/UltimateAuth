using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;

internal sealed class EfCoreAuthenticationSecurityStateStoreFactory<TDbContext> : IAuthenticationSecurityStateStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EfCoreAuthenticationSecurityStateStoreFactory(TDbContext db)
    {
        _db = db;
    }

    public IAuthenticationSecurityStateStore Create(TenantKey tenant)
    {
        return new EfCoreAuthenticationSecurityStateStore<TDbContext>(_db, new TenantContext(tenant));
    }
}
