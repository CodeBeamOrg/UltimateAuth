using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserIdentifierStoreFactory<TDbContext> : IUserIdentifierStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly IUAuthPaginationPolicy _pagination;

    public EfCoreUserIdentifierStoreFactory(TDbContext db, IUAuthPaginationPolicy pagination)
    {
        _db = db;
        _pagination = pagination;
    }

    public IUserIdentifierStore Create(TenantKey tenant)
    {
        return new EfCoreUserIdentifierStore<TDbContext>(_db, new TenantExecutionContext(tenant), _pagination);
    }
}
