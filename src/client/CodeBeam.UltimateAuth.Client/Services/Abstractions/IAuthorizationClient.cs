using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface IAuthorizationClient
{
    Task<UAuthResult<AuthorizationResult>> CheckAsync(AuthorizationCheckRequest request);
    Task<UAuthResult<UserRolesResponse>> GetMyRolesAsync(PageRequest? request = null);
    Task<UAuthResult<UserRolesResponse>> GetUserRolesAsync(UserKey userKey, PageRequest? request = null);
    Task<UAuthResult> AssignRoleToUserAsync(AssignRoleRequest request);
    Task<UAuthResult> RemoveRoleFromUserAsync(RemoveRoleRequest request);

    Task<UAuthResult<RoleInfo>> CreateRoleAsync(CreateRoleRequest request);
    Task<UAuthResult<PagedResult<RoleInfo>>> QueryRolesAsync(RoleQuery request);
    Task<UAuthResult> RenameRoleAsync(RenameRoleRequest request);
    Task<UAuthResult> SetRolePermissionsAsync(SetRolePermissionsRequest request);
    Task<UAuthResult<DeleteRoleResult>> DeleteRoleAsync(DeleteRoleRequest request);
}
