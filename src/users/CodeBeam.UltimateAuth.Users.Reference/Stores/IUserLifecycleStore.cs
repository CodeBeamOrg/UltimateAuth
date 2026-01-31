using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public interface IUserLifecycleStore
    {
        Task<bool> ExistsAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);

        Task<UserLifecycle?> GetAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);

        Task<PagedResult<UserLifecycle>> QueryAsync(string? tenantId, UserLifecycleQuery query, CancellationToken ct = default);

        Task CreateAsync(string? tenantId, UserLifecycle lifecycle, CancellationToken ct = default);

        Task ChangeStatusAsync(string? tenantId, UserKey userKey, UserStatus newStatus, DateTimeOffset updatedAt, CancellationToken ct = default);

        Task ChangeSecurityStampAsync(string? tenantId, UserKey userKey, Guid newSecurityStamp, DateTimeOffset updatedAt, CancellationToken ct = default);

        Task DeleteAsync(string? tenantId, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default);
    }
}
