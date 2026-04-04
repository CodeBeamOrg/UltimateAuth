using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IUserRoleService
{
    Task AssignAsync(AccessContext context, UserKey targetUserKey, string roleName, CancellationToken ct = default);
    Task RemoveAsync(AccessContext context, UserKey targetUserKey, string roleName, CancellationToken ct = default);
    Task<PagedResult<UserRoleInfo>> GetRolesAsync(AccessContext context, UserKey targetUserKey, PageRequest request, CancellationToken ct = default);
}
