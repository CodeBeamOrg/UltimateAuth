using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IUserPermissionService
{
    Task<UserPermissionsResult> GetMyPermissionsAsync(AccessContext context, CancellationToken ct = default);
}
