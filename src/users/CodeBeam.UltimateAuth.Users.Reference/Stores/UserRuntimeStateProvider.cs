using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Contracts;

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
        var userLifecycleKey = new UserLifecycleKey(tenant, userKey);
        var lifecycle = await _lifecycleStore.GetAsync(userLifecycleKey, ct);

        if (lifecycle is null)
            return null;

        return new UserRuntimeRecord
        {
            UserKey = lifecycle.UserKey,
            IsActive = lifecycle.Status == UserStatus.Active,
            CanAuthenticate = lifecycle.Status == UserStatus.Active || lifecycle.Status == UserStatus.SelfSuspended || lifecycle.Status == UserStatus.Suspended,
            IsDeleted = lifecycle.IsDeleted,
            Exists = true
        };
    }
}
