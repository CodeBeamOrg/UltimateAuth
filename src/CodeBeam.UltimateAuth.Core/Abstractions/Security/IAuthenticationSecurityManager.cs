using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IAuthenticationSecurityManager
{
    Task<AuthenticationSecurityState> GetOrCreateAccountAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
    Task<AuthenticationSecurityState> GetOrCreateFactorAsync(TenantKey tenant, UserKey userKey, CredentialType type, CancellationToken ct = default);
    Task UpdateAsync(AuthenticationSecurityState updated, long expectedVersion, CancellationToken ct = default);
    Task DeleteAsync(TenantKey tenant, UserKey userKey, AuthenticationSecurityScope scope, CredentialType? credentialType, CancellationToken ct = default);
}
