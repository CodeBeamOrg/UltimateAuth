using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserProfileStoreFactory<TDbContext> : IUserProfileStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly IUAuthPaginationPolicy _pagination;

    public EfCoreUserProfileStoreFactory(TDbContext db, IUAuthPaginationPolicy pagination)
    {
        _db = db;
        _pagination = pagination;
    }

    public IUserProfileStore Create(TenantKey tenant)
    {
        return new EfCoreUserProfileStore<TDbContext>(_db, new TenantExecutionContext(tenant), _pagination);
    }
}
