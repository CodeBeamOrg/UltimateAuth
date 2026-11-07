using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

internal sealed class EfCorePasswordHistoryStoreFactory<TDbContext>(TDbContext db) : IPasswordHistoryStoreFactory where TDbContext : DbContext
{
    public IPasswordHistoryStore Create(TenantKey tenant)
    {
        return new EfCorePasswordHistoryStore<TDbContext>(db, tenant);
    }
}
