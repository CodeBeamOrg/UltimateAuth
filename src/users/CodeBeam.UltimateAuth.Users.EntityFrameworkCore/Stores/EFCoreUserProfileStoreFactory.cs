using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserProfileStoreFactory : IUserProfileStoreFactory
{
    private readonly IDbContextFactory<UAuthUserDbContext> _dbFactory;

    public EfCoreUserProfileStoreFactory(IDbContextFactory<UAuthUserDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public IUserProfileStore Create(TenantKey tenant)
    {
        return new EfCoreUserProfileStore(_dbFactory, new TenantContext(tenant));
    }
}
