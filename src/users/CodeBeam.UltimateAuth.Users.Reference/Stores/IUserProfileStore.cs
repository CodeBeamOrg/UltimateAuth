using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference.Domain;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserProfileStore
{
    Task<ReferenceUserProfile?> GetAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);
    Task UpdateAsync(string? tenantId, UserKey userKey, UpdateProfileRequest request, CancellationToken ct = default);
    Task SetStatusAsync(string? tenantId, UserKey userKey, UserStatus status, CancellationToken ct = default);
    Task DeleteAsync(string? tenantId, UserKey userKey, UserDeleteMode mode, CancellationToken ct = default);
}
