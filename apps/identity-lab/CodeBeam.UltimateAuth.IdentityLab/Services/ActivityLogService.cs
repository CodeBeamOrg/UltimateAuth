using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class ActivityLogService
{
    private readonly IDbContextFactory<LabDbContext> _dbFactory;
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(IDbContextFactory<LabDbContext> dbFactory, ILogger<ActivityLogService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task RecordAsync(Guid sandboxId, string actorUserKey, string eventType, string? targetUserKey = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        db.ActivityLogs.Add(new ActivityLog
        {
            SandboxId = sandboxId,
            ActorUserKey = actorUserKey,
            EventType = eventType,
            TargetUserKey = targetUserKey
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task TryRecordAsync(Guid sandboxId, string actorUserKey, string eventType, string? targetUserKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await RecordAsync(sandboxId, actorUserKey, eventType, targetUserKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record activity {EventType} for sandbox {SandboxId}", eventType, sandboxId);
        }
    }

    public async Task<List<ActivityLog>> GetRecentAsync(UserKey userKey, int take = 100, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.ActivityLogs
            .AsNoTracking()
            .Where(x =>
                x.Sandbox.Members.Any(m => m.UserKey == userKey) &&
                x.Sandbox.Status == SandboxStatus.Active)
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);
    }
}
