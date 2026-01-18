using CodeBeam.UltimateAuth.Users;

namespace CodeBeam.UltimateAuth.Users.InMemory
{
    internal sealed class InMemoryUserSecurityStateProvider<TUserId> : IUserSecurityStateProvider<TUserId>
    {
        public Task<IUserSecurityState?> GetAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
        {
            // InMemory default: no MFA, no lockout, no risk signals
            return Task.FromResult<IUserSecurityState?>(null);
        }
    }
}
