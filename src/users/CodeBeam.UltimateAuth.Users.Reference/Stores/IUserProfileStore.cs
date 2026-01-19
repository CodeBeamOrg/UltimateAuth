using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference.Domain;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserProfileStore
{
    // TODO: Do CreateAsync internal with initializer service
    Task CreateAsync(string? tenantId, ReferenceUserProfile profile, CancellationToken ct = default);
    Task<ReferenceUserProfile?> GetAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);
    Task UpdateAsync(string? tenantId, UserKey userKey, UpdateProfileRequest request, CancellationToken ct = default);
    Task SetStatusAsync(string? tenantId, UserKey userKey, UserStatus status, CancellationToken ct = default);
    Task DeleteAsync(string? tenantId, UserKey userKey, DeleteMode mode, CancellationToken ct = default);
}
