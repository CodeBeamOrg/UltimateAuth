using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IAuthenticationSecurityStateStore
{
    Task<AuthenticationSecurityState?> GetAsync(TenantKey tenant, UserKey userKey, AuthenticationSecurityScope scope, CredentialType? credentialType, CancellationToken ct = default);

    Task AddAsync(AuthenticationSecurityState state, CancellationToken ct = default);

    Task UpdateAsync(AuthenticationSecurityState state, long expectedVersion, CancellationToken ct = default);
}
