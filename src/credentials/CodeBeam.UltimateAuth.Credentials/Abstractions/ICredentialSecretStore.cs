using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ICredentialSecretStore<TUserId>
{
    Task SetAsync(TenantKey tenant, TUserId userId, CredentialType type, string secretHash, CancellationToken ct = default);
}
