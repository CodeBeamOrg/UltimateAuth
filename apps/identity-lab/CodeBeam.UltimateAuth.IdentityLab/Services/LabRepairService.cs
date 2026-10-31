using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabRepairService(
    IDbContextFactory<LabDbContext> dbFactory,
    IUserSummaryQueryStoreFactory userQueryFactory,
    IRoleStoreFactory roleStoreFactory,
    IUserRoleStoreFactory userRoleStoreFactory,
    ILabAdminGuard adminGuard,
    ILogger<LabRepairService> logger)
{
    private const int PageSize = 100;
    private const int MaxScan = 5000;

    public async Task<RepairSnapshot> GetSnapshotAsync(AccessContext context, CancellationToken ct = default)
    {
        await adminGuard.EnsureAdminAsync(context, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var memberKeys = (await db.SandboxMembers
            .AsNoTracking()
            .Select(x => x.UserKey)
            .ToListAsync(ct))
            .ToHashSet();

        var owned = await db.Sandboxes
            .AsNoTracking()
            .Select(x => new { x.OwnerUserKey, x.Id })
            .ToListAsync(ct);

        var ownerMap = owned.ToDictionary(
            x => x.OwnerUserKey,
            x => x.Id);

        var emptySandboxes = await db.Sandboxes
            .AsNoTracking()
            .Where(x => !x.Members.Any())
            .OrderBy(x => x.CreatedAt)
            .Select(x => new EmptySandboxItem(
                x.Id,
                x.Name,
                x.OwnerUserKey,
                x.Status,
                x.CreatedAt))
            .ToListAsync(ct);

        var userStore = userQueryFactory.Create(TenantKeys.Single);
        var roleStore = roleStoreFactory.Create(TenantKeys.Single);
        var assignmentStore = userRoleStoreFactory.Create(TenantKeys.Single);

        var ownerRole = await roleStore.GetByNameAsync(
            LabRoles.Owner.ToUpperInvariant(), ct);

        var userRole = await roleStore.GetByNameAsync(
            LabRoles.User.ToUpperInvariant(), ct);

        var adminRole = await roleStore.GetByNameAsync(
            LabRoles.Admin.ToUpperInvariant(), ct);

        var orphanUsers = new List<OrphanUserItem>();
        var scanned = 0;
        var page = 1;
        var complete = false;

        while (scanned < MaxScan)
        {
            var users = await userStore.QueryAsync(
                new UserQuery
                {
                    PageNumber = page,
                    PageSize = PageSize,
                    IncludeDeleted = false
                },
                ct);

            if (users.Items.Count == 0)
            {
                complete = true;
                break;
            }

            foreach (var user in users.Items)
            {
                if (memberKeys.Contains(user.UserKey))
                    continue;

                var assignments = await assignmentStore
                    .GetAssignmentsAsync(user.UserKey, ct);

                var roleIds = assignments.Select(x => x.RoleId).ToHashSet();

                // Never offer platform admins for sandbox repair.
                if (adminRole is not null && roleIds.Contains(adminRole.Id))
                    continue;

                var role = ownerRole is not null && roleIds.Contains(ownerRole.Id)
                    ? LabRoles.Owner
                    : userRole is not null && roleIds.Contains(userRole.Id)
                        ? LabRoles.User
                        : "Unknown";

                ownerMap.TryGetValue(user.UserKey, out var ownedId);

                orphanUsers.Add(new OrphanUserItem(
                    user.UserKey,
                    user.DisplayName ?? user.UserName ?? user.UserKey.Value,
                    user.PrimaryEmail,
                    role,
                    user.CreatedAt,
                    ownedId == Guid.Empty ? null : ownedId));
            }

            scanned += users.Items.Count;

            if (scanned >= users.TotalCount)
            {
                complete = true;
                break;
            }

            page++;
        }

        return new RepairSnapshot(
            orphanUsers,
            emptySandboxes,
            complete);
    }

    public async Task<RepairOperationResult> RepairOwnerAsync(AccessContext context, UserKey ownerUserKey, CancellationToken ct = default)
    {
        await adminGuard.EnsureAdminAsync(context, ct);

        if (context.ActorUserKey is null || context.ActorUserKey == ownerUserKey)
        {
            return RepairOperationResult.Fail("Cannot repair the current administrator account.");
        }

        var roleStore = roleStoreFactory.Create(TenantKeys.Single);
        var assignmentStore = userRoleStoreFactory.Create(TenantKeys.Single);
        var userStore = userQueryFactory.Create(TenantKeys.Single);

        var ownerRole = await roleStore.GetByNameAsync(
            LabRoles.Owner.ToUpperInvariant(), ct);

        var adminRole = await roleStore.GetByNameAsync(
            LabRoles.Admin.ToUpperInvariant(), ct);

        if (ownerRole is null)
            return RepairOperationResult.Fail("Owner role is missing.");

        var users = await userStore.GetByKeysAsync([ownerUserKey], ct);

        if (users.Count != 1)
            return RepairOperationResult.Fail("User not found.");

        var assignments = await assignmentStore.GetAssignmentsAsync(
            ownerUserKey, ct);

        if (!assignments.Any(x => x.RoleId == ownerRole.Id) ||
            (adminRole is not null &&
             assignments.Any(x => x.RoleId == adminRole.Id)))
        {
            return RepairOperationResult.Fail(
                "User is not an eligible Sandbox Owner.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        await using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var existingMembership = await db.SandboxMembers
            .AnyAsync(x => x.UserKey == ownerUserKey, ct);

        if (existingMembership)
            return RepairOperationResult.Fail(
                "User already has a Sandbox membership.");

        var sandbox = await db.Sandboxes
            .Include(x => x.Settings)
            .FirstOrDefaultAsync(x => x.OwnerUserKey == ownerUserKey, ct);

        if (sandbox is null)
        {
            var name = users[0].DisplayName
                ?? users[0].UserName
                ?? "Recovered";

            if (name.Length > 120)
                name = name[..120];

            sandbox = new Sandbox
            {
                Id = Guid.NewGuid(),
                Name = $"{name}'s Sandbox",
                OwnerUserKey = ownerUserKey,
                Status = SandboxStatus.Passive,
                Settings = new SandboxSettings
                {
                    MaxUsers = SandboxLimits.DefaultMaxUsers
                }
            };

            db.Sandboxes.Add(sandbox);
        }

        // An existing sandbox may have other members.
        // Never exceed its configured capacity.
        var memberCount = await db.SandboxMembers
            .CountAsync(x => x.SandboxId == sandbox.Id, ct);

        if (memberCount >= sandbox.Settings.MaxUsers)
            return RepairOperationResult.Fail(
                "Sandbox capacity is full.");

        db.SandboxMembers.Add(new SandboxMember
        {
            Id = Guid.NewGuid(),
            SandboxId = sandbox.Id,
            UserKey = ownerUserKey,
            Role = SandboxMemberRole.Owner,
            JoinedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogWarning(
            "Ladmin {Actor} repaired owner {Owner} in sandbox {Sandbox}",
            context.ActorUserKey.Value.Value,
            ownerUserKey.Value,
            sandbox.Id);

        return RepairOperationResult.Ok(
            "Owner membership repaired. Review the sandbox status before activation.");
    }

    public async Task<RepairOperationResult> DeleteEmptySandboxAsync(
        AccessContext context,
        Guid sandboxId,
        CancellationToken ct = default)
    {
        await adminGuard.EnsureAdminAsync(context, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        await using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var sandbox = await db.Sandboxes
            .Include(x => x.Settings)
            .FirstOrDefaultAsync(x => x.Id == sandboxId, ct);

        if (sandbox is null)
            return RepairOperationResult.Fail("Sandbox not found.");

        if (await db.SandboxMembers.AnyAsync(x => x.SandboxId == sandboxId, ct))
        {
            return RepairOperationResult.Fail("Sandbox is not empty.");
        }

        // Activity logs may reference the sandbox.
        // Do not cascade-delete forensic records.
        if (await db.ActivityLogs.AnyAsync(x => x.SandboxId == sandboxId, ct))
        {
            return RepairOperationResult.Fail("Sandbox has activity history and cannot be deleted automatically.");
        }

        db.SandboxSettings.Remove(sandbox.Settings);
        db.Sandboxes.Remove(sandbox);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogWarning("Ladmin {Actor} deleted empty sandbox {Sandbox}",
            context.ActorUserKey!.Value.Value,
            sandboxId);

        return RepairOperationResult.Ok("Empty sandbox deleted.");
    }
}
