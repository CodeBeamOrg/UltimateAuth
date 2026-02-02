using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public interface IUserLifecycleStore
    {
        Task<bool> ExistsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);

        Task<UserLifecycle?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);

        Task<PagedResult<UserLifecycle>> QueryAsync(TenantKey tenant, UserLifecycleQuery query, CancellationToken ct = default);

        Task CreateAsync(TenantKey tenant, UserLifecycle lifecycle, CancellationToken ct = default);

        Task ChangeStatusAsync(TenantKey tenant, UserKey userKey, UserStatus newStatus, DateTimeOffset updatedAt, CancellationToken ct = default);

        Task ChangeSecurityStampAsync(TenantKey tenant, UserKey userKey, Guid newSecurityStamp, DateTimeOffset updatedAt, CancellationToken ct = default);

        Task DeleteAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default);
    }
}
