using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserSecurityStateProvider<TUserId> : IUserSecurityStateProvider<TUserId>
{
    public Task<IUserSecurityState?> GetAsync(TenantKey tenant, TUserId userId, CancellationToken ct = default)
    {
        // InMemory default: no MFA, no lockout, no risk signals
        return Task.FromResult<IUserSecurityState?>(null);
    }
}
