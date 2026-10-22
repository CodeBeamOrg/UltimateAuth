using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class SandboxService(LabDbContext db)
{
    public async Task<Sandbox> CreateAsync(UserKey ownerUserKey, string name, CancellationToken cancellationToken = default)
    {
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

    public async Task<bool> RenameAsync(UserKey ownerUserKey, string name, CancellationToken cancellationToken = default)
    {
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

    public Task<Sandbox?> GetByIdAsync(Guid sandboxId, CancellationToken cancellationToken = default)
    {
        return db.Sandboxes
            .AsNoTracking()
            .Include(x => x.Settings)
            .FirstOrDefaultAsync(x => x.Id == sandboxId, cancellationToken);
    }

    public Task<Sandbox?> GetByUserAsync(UserKey userKey, CancellationToken cancellationToken = default)
    {
        return db.Sandboxes
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

    public Task<List<SandboxMember>> GetMembersAsync(Guid sandboxId, CancellationToken cancellationToken = default)
    {
        return db.SandboxMembers
            .AsNoTracking()
            .Where(x => x.SandboxId == sandboxId)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetRemainingCapacityAsync(Guid sandboxId, CancellationToken cancellationToken = default)
    {
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
