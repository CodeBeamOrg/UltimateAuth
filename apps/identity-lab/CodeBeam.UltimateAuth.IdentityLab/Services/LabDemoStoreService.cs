using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed record DemoProduct(int Id, string Name, decimal Price);

public sealed class LabDemoStoreService(
    LabAccessContextProvider contextProvider,
    IDbContextFactory<LabDbContext> dbFactory,
    IRoleStoreFactory roleStoreFactory,
    IUserRoleStoreFactory userRoleStoreFactory)
{
    public async Task<IReadOnlySet<string>> GetPermissionsAsync(
        CancellationToken ct = default)
    {
        var context = await contextProvider.CreateAsync(
            LabDemoPermissions.ProductsRead,
            "demo.products",
            ct);

        if (!context.IsAuthenticated ||
            context.ActorUserKey is not UserKey actorKey)
        {
            return new HashSet<string>();
        }

        await using var db =
            await dbFactory.CreateDbContextAsync(ct);

        // A suspended sandbox cannot use the playground.
        var activeMember = await db.SandboxMembers
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserKey == actorKey &&
                x.Sandbox.Status ==
                    CodeBeam.UltimateAuth.IdentityLab.Domain.SandboxStatus.Active,
                ct);

        if (!activeMember)
            return new HashSet<string>();

        var userRoleStore =
            userRoleStoreFactory.Create(TenantKeys.Single);

        var assignments =
            await userRoleStore.GetAssignmentsAsync(actorKey, ct);

        var demoIds = new[]
        {
            LabRoleIds.Manager,
            LabRoleIds.Customer
        };

        var assignedIds = assignments
            .Select(x => x.RoleId)
            .Where(demoIds.Contains)
            .Distinct()
            .ToArray();

        var roleStore = roleStoreFactory.Create(TenantKeys.Single);
        var roles = await roleStore.GetByIdsAsync(assignedIds, ct);

        var permissions = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            if (role.IsDeleted)
                continue;

            // Fixed role identity: no other global roles participate.
            if (role.Id != LabRoleIds.Manager &&
                role.Id != LabRoleIds.Customer)
                continue;

            foreach (var permission in role.Permissions)
            {
                if (permission.IsWildcard || permission.IsPrefix)
                    continue;

                if (LabDemoPermissions.Manager.Contains(permission.Value) ||
                    LabDemoPermissions.Customer.Contains(permission.Value))
                {
                    permissions.Add(permission.Value);
                }
            }
        }

        return permissions;
    }

    public async Task RequireAsync(
        string permission,
        CancellationToken ct = default)
    {
        var permissions = await GetPermissionsAsync(ct);

        if (!permissions.Contains(permission))
            throw new UnauthorizedAccessException(
                $"Demo permission '{permission}' is required.");
    }
}
