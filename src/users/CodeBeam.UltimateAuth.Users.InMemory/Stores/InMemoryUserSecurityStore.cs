using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserSecurityStore<TUserId> : IUserSecurityStateDebugView<TUserId> where TUserId : notnull
{
    private readonly ConcurrentDictionary<(TenantKey, TUserId), InMemoryUserSecurityState> _states = new();

    public InMemoryUserSecurityState? Get(TenantKey tenant, TUserId userId)
        => _states.TryGetValue((tenant, userId), out var state) ? state : null;

    public void Set(TenantKey tenant, TUserId userId, InMemoryUserSecurityState state)
        => _states[(tenant, userId)] = state;

    public void Clear(TenantKey tenant, TUserId userId)
        => _states.TryRemove((tenant, userId), out _);

    public IUserSecurityState? GetState(TenantKey tenant, TUserId userId)
        => Get(tenant, userId);
}
