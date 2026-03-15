using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ICredentialProvider
{
    CredentialType Type { get; }

    Task<IReadOnlyCollection<ICredential>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);

    Task<bool> ValidateAsync(ICredential credential, string secret, CancellationToken ct = default);
}
