using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface IAuthorizationClient
{
    Task<UAuthResult<AuthorizationResult>> CheckAsync(AuthorizationCheckRequest request);

    Task<UAuthResult<UserRolesResponse>> GetMyRolesAsync();

    Task<UAuthResult<UserRolesResponse>> GetUserRolesAsync(UserKey userKey);

    Task<UAuthResult> AssignRoleAsync(UserKey userKey, string role);

    Task<UAuthResult> RemoveRoleAsync(UserKey userKey, string role);
}
