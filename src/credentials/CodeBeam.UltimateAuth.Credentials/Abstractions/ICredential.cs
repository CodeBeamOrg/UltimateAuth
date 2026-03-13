using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ICredential
{
    Guid Id { get; }
    TenantKey Tenant { get; init; }
    UserKey UserKey { get; init; }
    CredentialType Type { get; }
}
