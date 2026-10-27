using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed record SandboxRepairPreview(
    int TotalMemberships,
    int StaleMemberships);

public sealed record SandboxRepairResult(int RemovedMemberships);

public sealed class SandboxRepairService(IDbContextFactory<LabDbContext> dbFactory, IUserSummaryQueryStoreFactory userSummaryQueryStoreFactory, ILogger<SandboxRepairService> logger)
{
    public async Task<SandboxRepairPreview> PreviewAsync(UserKey ownerUserKey, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var sandbox = await db.Sandboxes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OwnerUserKey == ownerUserKey,
                ct)
            ?? throw new UnauthorizedAccessException(
                "Sandbox owner required.");

        var members = await db.SandboxMembers
            .AsNoTracking()
            .Where(x => x.SandboxId == sandbox.Id)
            .ToListAsync(ct);

        var candidates = members
            .Where(x =>
                x.Role == SandboxMemberRole.Member &&
                x.UserKey != sandbox.OwnerUserKey)
            .ToArray();

        var userStore = userSummaryQueryStoreFactory.Create(
            TenantKeys.Single);

        var users = await userStore.GetByKeysAsync(
            candidates.Select(x => x.UserKey).ToArray(),
            ct);

        var validKeys = users
            .Select(x => x.UserKey)
            .ToHashSet();

        return new SandboxRepairPreview(
            members.Count,
            candidates.Count(x => !validKeys.Contains(x.UserKey)));
    }

    public async Task<SandboxRepairResult> RepairAsync(UserKey ownerUserKey, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        await using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var sandbox = await db.Sandboxes
            .SingleOrDefaultAsync(
                x => x.OwnerUserKey == ownerUserKey,
                ct)
            ?? throw new UnauthorizedAccessException(
                "Sandbox owner required.");

        if (sandbox.Status != SandboxStatus.Active)
            throw new InvalidOperationException(
                "Sandbox is not active.");

        var candidates = await db.SandboxMembers
            .Where(x =>
                x.SandboxId == sandbox.Id &&
                x.Role == SandboxMemberRole.Member &&
                x.UserKey != sandbox.OwnerUserKey)
            .ToListAsync(ct);

        var userStore = userSummaryQueryStoreFactory.Create(
            TenantKeys.Single);

        var users = await userStore.GetByKeysAsync(
            candidates.Select(x => x.UserKey).ToArray(),
            ct);

        var validKeys = users
            .Select(x => x.UserKey)
            .ToHashSet();

        var staleMembers = candidates
            .Where(x => !validKeys.Contains(x.UserKey))
            .ToArray();

        if (staleMembers.Length > 0)
        {
            db.SandboxMembers.RemoveRange(staleMembers);
            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);

        logger.LogInformation(
            "Sandbox repair completed. Owner={Owner}, " +
            "Sandbox={SandboxId}, Removed={Count}",
            ownerUserKey.Value,
            sandbox.Id,
            staleMembers.Length);

        return new SandboxRepairResult(staleMembers.Length);
    }
}
