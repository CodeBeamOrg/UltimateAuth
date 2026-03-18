using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserIdentifierStoreFactory : IUserIdentifierStoreFactory
{
    private readonly IDbContextFactory<UAuthUserDbContext> _dbFactory;

    public EfCoreUserIdentifierStoreFactory(IDbContextFactory<UAuthUserDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public IUserIdentifierStore Create(TenantKey tenant)
    {
        return new EfCoreUserIdentifierStore(_dbFactory, new TenantContext(tenant));
    }
}
