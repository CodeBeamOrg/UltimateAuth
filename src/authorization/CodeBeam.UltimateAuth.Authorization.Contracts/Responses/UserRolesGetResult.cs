using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record UserRolesGetResult
{
    public required UserKey UserKey { get; init; }
    public required PagedResult<UserRoleInfo> Roles { get; init; }
}
