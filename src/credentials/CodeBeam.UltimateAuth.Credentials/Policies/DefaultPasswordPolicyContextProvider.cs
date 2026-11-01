using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials.Policies;

public sealed class DefaultPasswordPolicyContextProvider : IPasswordPolicyContextProvider
{
    public Task<PasswordPolicyContext> CreateAsync(TenantKey tenant, UserKey userKey, string password, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(new PasswordPolicyContext
        {
            Password = password
        });
    }
}
