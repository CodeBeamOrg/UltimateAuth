using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

internal sealed class EfCorePasswordCredentialStoreFactory : IPasswordCredentialStoreFactory
{
    private readonly UAuthCredentialDbContext _db;

    public EfCorePasswordCredentialStoreFactory(UAuthCredentialDbContext db)
    {
        _db = db;
    }

    public IPasswordCredentialStore Create(TenantKey tenant)
    {
        return new EfCorePasswordCredentialStore(_db, new TenantContext(tenant));
    }
}
