using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserSecurityStore : IUserSecurityStateDebugView
{
    private readonly ConcurrentDictionary<(TenantKey, UserKey), InMemoryUserSecurityState> _states = new();

    public InMemoryUserSecurityState? Get(TenantKey tenant, UserKey userKey)
        => _states.TryGetValue((tenant, userKey), out var state) ? state : null;

    public void Set(TenantKey tenant, UserKey userKey, InMemoryUserSecurityState state)
        => _states[(tenant, userKey)] = state;

    public void Clear(TenantKey tenant, UserKey userKey)
        => _states.TryRemove((tenant, userKey), out _);

    public IUserSecurityState? GetState(TenantKey tenant, UserKey userKey)
        => Get(tenant, userKey);
}
