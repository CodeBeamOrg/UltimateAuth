using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public interface IUserLifecycleStore
    {
        Task CreateAsync(string? tenantId, UserRecord<UserKey> user, CancellationToken ct = default);
        Task UpdateStatusAsync(string? tenantId, UserKey userKey, UserStatus status, CancellationToken ct = default);
        Task MarkDeletedAsync(string? tenantId, UserKey userKey, DateTimeOffset deletedAt, CancellationToken ct = default);
        Task DeleteAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);
    }
}
