using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabRootResolver : ILabRootResolver
{
    public bool IsLadmin(AccessContext context)
    {
        if (!context.IsAuthenticated || context.ActorUserKey is null)
        {
            return false;
        }

        if (!context.Attributes.TryGetValue("roles", out var value))
        {
            return false;
        }

        var result = value is IEnumerable<RoleId> roleIds && roleIds.Contains(LabRoleIds.Admin);
        return result;
    }
}
