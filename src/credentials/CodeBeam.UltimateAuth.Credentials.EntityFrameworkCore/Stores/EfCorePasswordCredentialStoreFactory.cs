using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

internal sealed class EfCorePasswordCredentialStoreFactory<TDbContext> : IPasswordCredentialStoreFactory where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EfCorePasswordCredentialStoreFactory(TDbContext db)
    {
        _db = db;
    }

    public IPasswordCredentialStore Create(TenantKey tenant)
    {
        return new EfCorePasswordCredentialStore<TDbContext>(_db, new TenantExecutionContext(tenant));
    }
}
