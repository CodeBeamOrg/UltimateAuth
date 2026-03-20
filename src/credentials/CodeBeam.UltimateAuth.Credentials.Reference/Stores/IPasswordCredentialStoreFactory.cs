using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public interface IPasswordCredentialStoreFactory
{
    IPasswordCredentialStore Create(TenantKey tenant);
}
