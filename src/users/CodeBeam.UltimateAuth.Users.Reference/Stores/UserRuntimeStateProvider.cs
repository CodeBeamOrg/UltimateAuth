using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class UserRuntimeStateProvider : IUserRuntimeStateProvider
{
    private readonly IUserLifecycleStore _lifecycleStore;

    public UserRuntimeStateProvider(IUserLifecycleStore lifecycleStore)
    {
        _lifecycleStore = lifecycleStore;
    }

    public async Task<UserRuntimeRecord?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var lifecycle = await _lifecycleStore.GetAsync(tenant, userKey, ct);

        if (lifecycle is null)
            return null;

        return new UserRuntimeRecord
        {
            UserKey = lifecycle.UserKey,
            IsActive = lifecycle.Status == UserStatus.Active,
            IsDeleted = lifecycle.IsDeleted,
            Exists = true
        };
    }
}
