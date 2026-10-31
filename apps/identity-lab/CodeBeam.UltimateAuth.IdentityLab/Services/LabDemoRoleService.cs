using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabDemoRoleService(LabAccessContextProvider contextProvider, IDbContextFactory<LabDbContext> dbFactory,
                                       IRoleStoreFactory roleStoreFactory, IUserRoleStoreFactory userRoleStoreFactory)
{
    public async Task SetRoleAsync(UserKey targetUserKey, string roleName, bool assigned, CancellationToken ct = default)
    {
        // Never accept arbitrary roles.
        if (!LabRoles.IsDemoRole(roleName))
            throw new UnauthorizedAccessException("Only demo roles can be managed here.");

        var context = await contextProvider.CreateAsync(UAuthActions.Authorization.Roles.AssignAdmin, "authorization.roles", ct);

        if (!context.IsAuthenticated ||
            context.IsSystemActor ||
            context.ActorUserKey is not UserKey actorKey)
        {
            throw new UnauthorizedAccessException();
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Resolve ownership from the Sandbox database,
        // never from global authorization roles.
        var owner = await db.SandboxMembers
            .AsNoTracking()
            .Where(x =>
                x.UserKey == actorKey &&
                x.Role == SandboxMemberRole.Owner &&
                x.Sandbox.Status == SandboxStatus.Active)
            .Select(x => new { x.SandboxId })
            .SingleOrDefaultAsync(ct);

        if (owner is null)
            throw new UnauthorizedAccessException(
                "Active sandbox ownership is required.");

        // The target must be a regular member of this sandbox.
        var targetAllowed = await db.SandboxMembers
            .AsNoTracking()
            .AnyAsync(x =>
                x.SandboxId == owner.SandboxId &&
                x.UserKey == targetUserKey &&
                x.Role == SandboxMemberRole.Member,
                ct);

        if (!targetAllowed)
            throw new UnauthorizedAccessException("Target must be a member of your sandbox.");

        var roleStore = roleStoreFactory.Create(TenantKeys.Single);
        var userRoleStore = userRoleStoreFactory.Create(TenantKeys.Single);

        var expectedId = roleName.Equals(LabRoles.Manager,
            StringComparison.OrdinalIgnoreCase)
                ? LabRoleIds.Manager
                : LabRoleIds.Customer;

        var role = await roleStore.GetByNameAsync(roleName.ToUpperInvariant(), ct);

        if (role is null || role.IsDeleted || role.Id != expectedId)
        {
            throw new InvalidOperationException("Demo role is not correctly configured.");
        }

        var assignments = await userRoleStore.GetAssignmentsAsync(targetUserKey, ct);

        var alreadyAssigned = assignments.Any(x => x.RoleId == role.Id);

        if (assigned && !alreadyAssigned)
        {
            await userRoleStore.AssignAsync(targetUserKey, role.Id, DateTimeOffset.UtcNow, ct);
        }
        else if (!assigned && alreadyAssigned)
        {
            await userRoleStore.RemoveAsync(targetUserKey, role.Id, ct);
        }
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(UserKey targetUserKey, CancellationToken ct = default)
    {
        var context = await contextProvider.CreateAsync(UAuthActions.Authorization.Roles.GetAdmin, "authorization.roles", ct);

        if (!context.IsAuthenticated || context.IsSystemActor || context.ActorUserKey is not UserKey actorKey)
        {
            throw new UnauthorizedAccessException();
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var allowed = await db.SandboxMembers
            .AsNoTracking()
            .AnyAsync(actor =>
                actor.UserKey == actorKey &&
                actor.Role == SandboxMemberRole.Owner &&
                actor.Sandbox.Status == SandboxStatus.Active &&
                db.SandboxMembers.Any(target =>
                    target.SandboxId == actor.SandboxId &&
                    target.UserKey == targetUserKey &&
                    target.Role == SandboxMemberRole.Member),
                ct);

        if (!allowed)
            throw new UnauthorizedAccessException();

        var store = userRoleStoreFactory.Create(TenantKeys.Single);
        var assignments = await store.GetAssignmentsAsync(targetUserKey, ct);

        var result = new List<string>();

        if (assignments.Any(x => x.RoleId == LabRoleIds.Manager))
            result.Add(LabRoles.Manager);

        if (assignments.Any(x => x.RoleId == LabRoleIds.Customer))
            result.Add(LabRoles.Customer);

        return result;
    }
}
