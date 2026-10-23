using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class SandboxService(IDbContextFactory<LabDbContext> dbFactory)
{
    public async Task<Sandbox> CreateAsync(UserKey ownerUserKey, string name, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(ownerUserKey.Value))
            throw new ArgumentException("Owner user key is required.", nameof(ownerUserKey));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Sandbox name is required.", nameof(name));

        name = name.Trim();

        if (name.Length > 150)
            throw new ArgumentException("Sandbox name cannot exceed 150 characters.", nameof(name));

        var sandbox = new Sandbox
        {
            Id = Guid.NewGuid(),
            Name = name,
            OwnerUserKey = ownerUserKey,
            Status = SandboxStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            Settings = new SandboxSettings
            {
                MaxUsers = 5
            },
            Members =
            [
                new SandboxMember
                {
                    Id = Guid.NewGuid(),
                    UserKey = ownerUserKey,
                    Role = SandboxMemberRole.Owner,
                    JoinedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        db.Sandboxes.Add(sandbox);

        // OwnerUserKey and UserKey unique indexes enforce
        // ownership and membership uniqueness atomically.
        await db.SaveChangesAsync(cancellationToken);

        return sandbox;
    }

    public async Task<Sandbox> EnsureAsync(UserKey ownerUserKey, string name, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(ownerUserKey.Value))
            throw new ArgumentException("Owner user key is required.", nameof(ownerUserKey));

        var existing = await db.Sandboxes
            .Include(x => x.Settings)
            .Include(x => x.Members)
            .FirstOrDefaultAsync(
                x => x.OwnerUserKey == ownerUserKey,
                cancellationToken);

        if (existing is not null)
        {
            var ownerMember = existing.Members
                .FirstOrDefault(x => x.UserKey == ownerUserKey);

            if (ownerMember is null)
            {
                existing.Members.Add(new SandboxMember
                {
                    Id = Guid.NewGuid(),
                    UserKey = ownerUserKey,
                    Role = SandboxMemberRole.Owner
                });

                await db.SaveChangesAsync(cancellationToken);
            }
            else if (ownerMember.Role != SandboxMemberRole.Owner)
            {
                ownerMember.Role = SandboxMemberRole.Owner;
                await db.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        return await CreateAsync(
            ownerUserKey,
            name,
            cancellationToken);
    }

    public async Task<bool> RenameAsync(UserKey ownerUserKey, string name, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(ownerUserKey.Value))
            throw new ArgumentException("Owner user key is required.", nameof(ownerUserKey));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Sandbox name is required.", nameof(name));

        name = name.Trim();

        if (name.Length > 150)
            throw new ArgumentException("Sandbox name cannot exceed 150 characters.", nameof(name));

        var sandbox = await db.Sandboxes.FirstOrDefaultAsync(x => x.OwnerUserKey == ownerUserKey, cancellationToken);

        if (sandbox is null)
            return false;

        if (sandbox.Status != SandboxStatus.Active)
            throw new InvalidOperationException("Sandbox is not active.");

        sandbox.Name = name;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<SandboxMember> AddMemberAsync(UserKey ownerUserKey, UserKey newUserKey, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(ownerUserKey.Value))
            throw new ArgumentException("Owner user key is required.", nameof(ownerUserKey));

        if (string.IsNullOrWhiteSpace(newUserKey.Value))
            throw new ArgumentException("Member user key is required.", nameof(newUserKey));

        if (ownerUserKey == newUserKey)
            throw new InvalidOperationException("Sandbox owner cannot be added as a regular member.");

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        var sandbox = await db.Sandboxes
            .Include(x => x.Settings)
            .FirstOrDefaultAsync(x => x.OwnerUserKey == ownerUserKey, cancellationToken);

        if (sandbox is null)
            throw new InvalidOperationException("Sandbox not found.");

        if (sandbox.Status != SandboxStatus.Active)
            throw new InvalidOperationException("Sandbox is not active.");

        var existing = await db.SandboxMembers.FirstOrDefaultAsync(x => x.UserKey == newUserKey, cancellationToken);

        if (existing is not null)
        {
            if (existing.SandboxId != sandbox.Id || existing.Role != SandboxMemberRole.Member)
            {
                throw new InvalidOperationException("User already belongs to another sandbox or has an invalid membership.");
            }

            await transaction.CommitAsync(cancellationToken);
            return existing;
        }

        var memberCount = await db.SandboxMembers.CountAsync(x => x.SandboxId == sandbox.Id, cancellationToken);

        if (memberCount >= sandbox.Settings.MaxUsers)
            throw new InvalidOperationException("Sandbox user limit reached.");

        var member = new SandboxMember
        {
            Id = Guid.NewGuid(),
            SandboxId = sandbox.Id,
            UserKey = newUserKey,
            Role = SandboxMemberRole.Member,
            JoinedAt = DateTimeOffset.UtcNow
        };

        db.SandboxMembers.Add(member);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return member;
    }

    public async Task<Sandbox?> GetByIdAsync(Guid sandboxId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Sandboxes
            .AsNoTracking()
            .Include(x => x.Settings)
            .FirstOrDefaultAsync(x => x.Id == sandboxId, cancellationToken);
    }

    public async Task<Sandbox?> GetByUserAsync(UserKey userKey, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Sandboxes
            .AsNoTracking()
            .Include(x => x.Settings)
            .Where(x => x.Members.Any(m => m.UserKey == userKey))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SandboxAccess> GetAccessAsync(UserKey userKey, CancellationToken cancellationToken = default)
    {
        var sandbox = await GetByUserAsync(userKey, cancellationToken);

        return sandbox is null
            ? SandboxAccess.Denied
            : SandboxAccessPolicy.Evaluate(sandbox.Status);
    }

    public async Task<List<SandboxMember>> GetMembersAsync(Guid sandboxId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.SandboxMembers
            .AsNoTracking()
            .Where(x => x.SandboxId == sandboxId)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetRemainingCapacityAsync(Guid sandboxId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var settings = await db.SandboxSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.SandboxId == sandboxId,
                cancellationToken);

        if (settings is null)
            throw new InvalidOperationException("Sandbox settings not found.");

        var count = await db.SandboxMembers
            .CountAsync(x => x.SandboxId == sandboxId, cancellationToken);

        return Math.Max(0, settings.MaxUsers - count);
    }
}
