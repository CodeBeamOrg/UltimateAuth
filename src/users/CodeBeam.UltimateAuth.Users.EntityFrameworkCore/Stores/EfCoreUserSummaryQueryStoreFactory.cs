using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserSummaryQueryStoreFactory<TDbContext> : IUserSummaryQueryStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly IUAuthPaginationPolicy _pagination;

    public EfCoreUserSummaryQueryStoreFactory(TDbContext db, IUAuthPaginationPolicy pagination)
    {
        _db = db;
        _pagination = pagination;
    }

    public IUserSummaryQueryStore Create(TenantKey tenant)
    {
        return new EfCoreUserSummaryQueryStore<TDbContext>(_db, new TenantExecutionContext(tenant), _pagination);
    }
}
