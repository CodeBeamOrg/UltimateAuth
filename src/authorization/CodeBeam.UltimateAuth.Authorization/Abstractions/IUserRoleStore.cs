using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IUserRoleStore
{
    Task<IReadOnlyCollection<UserRole>> GetAssignmentsAsync(UserKey userKey, CancellationToken ct = default);
    Task AssignAsync(UserKey userKey, RoleId roleId, DateTimeOffset assignedAt, CancellationToken ct = default);
    Task RemoveAsync(UserKey userKey, RoleId roleId, CancellationToken ct = default);
    Task RemoveAssignmentsByRoleAsync(RoleId roleId, CancellationToken ct = default);
    Task<int> CountAssignmentsAsync(RoleId roleId, CancellationToken ct = default);
}
