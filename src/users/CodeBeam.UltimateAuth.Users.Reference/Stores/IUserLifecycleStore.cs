using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference.Domain;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public interface IUserLifecycleStore
    {
        Task CreateAsync(string? tenantId, UserProfile user, CancellationToken ct = default);
        Task UpdateStatusAsync(string? tenantId, UserKey userKey, UserStatus status, CancellationToken ct = default);
        Task DeleteAsync(string? tenantId, UserKey userKey, DeleteMode mode, DateTimeOffset at, CancellationToken ct = default);
    }
}
