using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal sealed class EfCoreRoleStoreFactory<TDbContext> : IRoleStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly IUAuthPaginationPolicy _pagination;

    public EfCoreRoleStoreFactory(TDbContext db, IUAuthPaginationPolicy pagination)
    {
        _db = db;
        _pagination = pagination;
    }

    public IRoleStore Create(TenantKey tenant)
    {
        return new EfCoreRoleStore<TDbContext>(_db, new TenantExecutionContext(tenant), _pagination);
    }
}
