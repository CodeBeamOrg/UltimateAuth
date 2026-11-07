using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials.Policies;

public interface IPasswordPolicyContextProvider
{
    Task<PasswordPolicyContext> CreateAsync(TenantKey tenant, UserKey userKey, string password, CancellationToken ct = default);
}
