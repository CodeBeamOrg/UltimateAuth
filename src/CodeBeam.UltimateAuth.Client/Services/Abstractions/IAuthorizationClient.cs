using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface IAuthorizationClient
{
    Task<UAuthResult<AuthorizationResult>> CheckAsync(AuthorizationCheckRequest request);
    Task<UAuthResult<UserRolesResponse>> GetMyRolesAsync(PageRequest? request = null);
    Task<UAuthResult<UserRolesResponse>> GetUserRolesAsync(UserKey userKey, PageRequest? request = null);
    Task<UAuthResult> AssignRoleToUserAsync(UserKey userKey, string role);
    Task<UAuthResult> RemoveRoleFromUserAsync(UserKey userKey, string role);

    Task<UAuthResult<RoleInfo>> CreateRoleAsync(CreateRoleRequest request);
    Task<UAuthResult<PagedResult<RoleInfo>>> QueryRolesAsync(RoleQuery request);
    Task<UAuthResult> RenameRoleAsync(RoleId roleId, RenameRoleRequest request);
    Task<UAuthResult> SetPermissionsAsync(RoleId roleId, SetPermissionsRequest request);
    Task<UAuthResult<DeleteRoleResult>> DeleteRoleAsync(RoleId roleId, DeleteRoleRequest request);
}
