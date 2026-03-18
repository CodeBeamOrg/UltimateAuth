using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;

internal sealed class EfCoreAuthenticationSecurityStateStoreFactory : IAuthenticationSecurityStateStoreFactory
{
    private readonly UAuthAuthenticationDbContext _db;

    public EfCoreAuthenticationSecurityStateStoreFactory(UAuthAuthenticationDbContext db)
    {
        _db = db;
    }

    public IAuthenticationSecurityStateStore Create(TenantKey tenant)
    {
        return new EfCoreAuthenticationSecurityStateStore(_db, new TenantContext(tenant));
    }
}
