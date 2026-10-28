using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabRootResolver : ILabRootResolver
{
    public bool IsLadmin(AccessContext context)
    {
        if (!context.IsAuthenticated ||
            context.IsSystemActor ||
            context.ActorUserKey is null ||
            context.ActorTenant.IsSystem ||
            context.ActorTenant.IsUnresolved ||
            context.IsCrossTenant)
        {
            return false;
        }

        return context.Attributes.TryGetValue("roles", out var value)
               && value is IEnumerable<RoleId> roleIds
               && roleIds.Contains(LabRoleIds.Admin);
    }
}
