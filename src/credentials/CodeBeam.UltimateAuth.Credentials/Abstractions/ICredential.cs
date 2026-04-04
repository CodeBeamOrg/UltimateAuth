using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ICredential
{
    Guid Id { get; }
    TenantKey Tenant { get; }
    UserKey UserKey { get; }
    CredentialType Type { get; }

    CredentialSecurityState Security { get; }
    CredentialMetadata Metadata { get; }

    DateTimeOffset CreatedAt { get; }
    DateTimeOffset? UpdatedAt { get; }
    DateTimeOffset? DeletedAt { get; }

    long Version { get; }

    bool IsDeleted { get; }
}
