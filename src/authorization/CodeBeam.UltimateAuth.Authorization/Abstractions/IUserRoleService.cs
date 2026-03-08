using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IUserRoleService
{
    Task AssignAsync(AccessContext context, UserKey targetUserKey, string roleName, CancellationToken ct = default);
    Task RemoveAsync(AccessContext context, UserKey targetUserKey, string roleName, CancellationToken ct = default);
    Task<IReadOnlyCollection<string>> GetRolesAsync(AccessContext context, UserKey targetUserKey, CancellationToken ct = default);
}
