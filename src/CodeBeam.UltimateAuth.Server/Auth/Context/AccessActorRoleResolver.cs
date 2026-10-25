using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class AccessActorRoleResolver(IUserRoleStoreFactory userRoleFactory, IRoleStoreFactory roleFactory)
{
    public async Task<ResolvedActorRoles> ResolveAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var userRoleStore = userRoleFactory.Create(tenant);
        var assignments = await userRoleStore.GetAssignmentsAsync(userKey, ct);
        var roleIds = assignments.Select(x => x.RoleId).Distinct().ToArray();

        if (roleIds.Length == 0)
            return new ResolvedActorRoles([], roleIds);

        var roleStore = roleFactory.Create(tenant);

        var roles = await roleStore.GetByIdsAsync(roleIds, ct);

        var actorRoles = roles
            .Where(x => !x.IsDeleted && x.Tenant == tenant)
            .Select(x => new AccessActorRole(x.Id.ToString(), x.Name))
            .ToArray();

        return new ResolvedActorRoles(actorRoles, roleIds);
    }
}

internal sealed record ResolvedActorRoles(
    IReadOnlyList<AccessActorRole> ActorRoles,
    IReadOnlyCollection<RoleId> RoleIds);