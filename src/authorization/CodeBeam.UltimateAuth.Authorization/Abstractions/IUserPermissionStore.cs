using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IUserPermissionStore
{
    Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);
}
