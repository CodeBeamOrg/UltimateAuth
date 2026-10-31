using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabOwnerRepairService(
    IDbContextFactory<LabDbContext> dbFactory,
    IUserLifecycleStoreFactory lifecycleStoreFactory,
    IRoleStoreFactory roleStoreFactory,
    IUserRoleStoreFactory userRoleStoreFactory,
    ILogger<LabOwnerRepairService> logger)
{
    public async Task<OwnerRepairStatus> GetStatusAsync(
        AccessContext context,
        CancellationToken ct = default)
    {
        var ownerKey = await GetEligibleOwnerAsync(context, ct);

        if (ownerKey is null)
        {
            return new OwnerRepairStatus(
                OwnerRepairState.NotEligible,
                "Your account is not eligible for automatic sandbox recovery.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var membership = await db.SandboxMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserKey == ownerKey.Value, ct);

        if (membership is not null)
        {
            return new OwnerRepairStatus(
                OwnerRepairState.NotRequired,
                "Your account already has a sandbox membership.");
        }

        var ownedSandbox = await db.Sandboxes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OwnerUserKey == ownerKey.Value, ct);

        if (ownedSandbox is not null)
        {
            var otherOwnerExists = await db.SandboxMembers.AnyAsync(
                x => x.SandboxId == ownedSandbox.Id &&
                     x.Role == SandboxMemberRole.Owner,
                ct);

            if (otherOwnerExists)
            {
                return new OwnerRepairStatus(
                    OwnerRepairState.NotEligible,
                    "Sandbox ownership requires administrator review.");
            }
        }

        return new OwnerRepairStatus(
            OwnerRepairState.CanRepair,
            ownedSandbox is null
                ? "Your account was created, but sandbox setup was not completed."
                : "Your sandbox exists, but your owner membership needs repair.");
    }

    public async Task<OwnerRepairResult> RepairAsync(
        AccessContext context,
        CancellationToken ct = default)
    {
        var ownerKey = await GetEligibleOwnerAsync(context, ct);

        if (ownerKey is null)
        {
            return OwnerRepairResult.Fail(
                "Your account is not eligible for automatic recovery.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        await using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        // Recheck under the write transaction.
        var membershipExists = await db.SandboxMembers
            .AnyAsync(x => x.UserKey == ownerKey.Value, ct);

        if (membershipExists)
        {
            return OwnerRepairResult.Fail(
                "Your account already belongs to a sandbox.");
        }

        var sandbox = await db.Sandboxes
            .Include(x => x.Settings)
            .FirstOrDefaultAsync(
                x => x.OwnerUserKey == ownerKey.Value,
                ct);

        if (sandbox is null)
        {
            sandbox = new Sandbox
            {
                Id = Guid.NewGuid(),
                Name = "My Sandbox",
                OwnerUserKey = ownerKey.Value,
                Status = SandboxStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                Settings = new SandboxSettings
                {
                    MaxUsers = SandboxLimits.DefaultMaxUsers
                }
            };

            db.Sandboxes.Add(sandbox);
        }
        else
        {
            var existingOwner = await db.SandboxMembers
                .AnyAsync(x =>
                    x.SandboxId == sandbox.Id &&
                    x.Role == SandboxMemberRole.Owner,
                    ct);

            if (existingOwner)
            {
                return OwnerRepairResult.Fail(
                    "Sandbox ownership requires administrator review.");
            }

            var memberCount = await db.SandboxMembers
                .CountAsync(x => x.SandboxId == sandbox.Id, ct);

            var capacity = Math.Min(
                sandbox.Settings.MaxUsers,
                SandboxLimits.AbsoluteMaxUsers);

            if (memberCount >= capacity)
            {
                return OwnerRepairResult.Fail(
                    "Sandbox capacity is full. Contact the administrator.");
            }
        }

        db.SandboxMembers.Add(new SandboxMember
        {
            Id = Guid.NewGuid(),
            SandboxId = sandbox.Id,
            UserKey = ownerKey.Value,
            Role = SandboxMemberRole.Owner,
            JoinedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogWarning(
            "Owner self-repair completed for {OwnerUserKey}, Sandbox {SandboxId}",
            ownerKey.Value.Value,
            sandbox.Id);

        return OwnerRepairResult.Ok(
            "Your sandbox setup has been completed successfully.");
    }

    private async Task<UserKey?> GetEligibleOwnerAsync(
        AccessContext context,
        CancellationToken ct)
    {
        if (!context.IsAuthenticated ||
            context.IsSystemActor ||
            context.ActorUserKey is not UserKey userKey ||
            context.ActorTenant.IsSystem ||
            context.ActorTenant.IsUnresolved ||
            context.IsCrossTenant ||
            context.ActorTenant != TenantKeys.Single)
        {
            return null;
        }

        var lifecycleStore = lifecycleStoreFactory.Create(context.ActorTenant);

        var lifecycle = await lifecycleStore.GetAsync(
            new UserLifecycleKey(context.ActorTenant, userKey),
            ct);

        if (lifecycle is null ||
            lifecycle.Status != UserStatus.Active)
        {
            return null;
        }

        var roleStore = roleStoreFactory.Create(context.ActorTenant);
        var userRoleStore = userRoleStoreFactory.Create(context.ActorTenant);

        var ownerRole = await roleStore.GetByNameAsync(
            LabRoles.Owner.ToUpperInvariant(), ct);

        var adminRole = await roleStore.GetByNameAsync(
            LabRoles.Admin.ToUpperInvariant(), ct);

        if (ownerRole is null || ownerRole.IsDeleted)
            return null;

        var assignments = await userRoleStore.GetAssignmentsAsync(
            userKey, ct);

        if (!assignments.Any(x => x.RoleId == ownerRole.Id))
            return null;

        // Platform admins must not provision their own sandbox
        // through the ordinary Owner recovery workflow.
        if (adminRole is not null &&
            assignments.Any(x => x.RoleId == adminRole.Id))
        {
            return null;
        }

        return userKey;
    }
}
