using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    internal sealed class UserRuntimeStore : IUserRuntimeStateProvider
    {
        private readonly IUserLifecycleStore _lifecycleStore;

        public UserRuntimeStore(IUserLifecycleStore lifecycleStore)
        {
            _lifecycleStore = lifecycleStore;
        }

        public async Task<UserRuntimeRecord?> GetAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
        {
            var lifecycle = await _lifecycleStore.GetAsync(tenantId, userKey, ct);

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
}
