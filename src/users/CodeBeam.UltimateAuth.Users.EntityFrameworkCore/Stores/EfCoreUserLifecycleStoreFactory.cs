using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserLifecycleStoreFactory<TDbContext> : IUserLifecycleStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly IUAuthPaginationPolicy _pagination;

    public EfCoreUserLifecycleStoreFactory(TDbContext db, IUAuthPaginationPolicy pagination)
    {
        _db = db;
        _pagination = pagination;
    }

    public IUserLifecycleStore Create(TenantKey tenant)
    {
        return new EfCoreUserLifecycleStore<TDbContext>(_db, new TenantExecutionContext(tenant), _pagination);
    }
}
