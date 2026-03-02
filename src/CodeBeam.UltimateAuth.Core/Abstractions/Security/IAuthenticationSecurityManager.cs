using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IAuthenticationSecurityManager
{
    Task<AuthenticationSecurityState> GetOrCreateAccountAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
    Task<AuthenticationSecurityState> GetOrCreateFactorAsync(TenantKey tenant, UserKey userKey, CredentialType type, CancellationToken ct = default);
    Task<AuthenticationSecurityState> RegisterFailureAsync(AuthenticationSecurityState state, DateTimeOffset now, CancellationToken ct = default);
    Task<AuthenticationSecurityState> RegisterSuccessAsync(AuthenticationSecurityState state, CancellationToken ct = default);
    Task<AuthenticationSecurityState> UnlockAsync(AuthenticationSecurityState state, CancellationToken ct = default);
}
