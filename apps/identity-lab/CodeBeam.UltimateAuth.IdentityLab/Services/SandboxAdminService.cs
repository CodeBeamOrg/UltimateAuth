using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class SandboxAdminService(LabDbContext db, ILabAdminGuard adminGuard)
{
    public async Task<bool> SetStatusAsync(AccessContext context, Guid sandboxId, SandboxStatus status, CancellationToken cancellationToken = default)
    {
        await adminGuard.EnsureAdminAsync(context, cancellationToken);

        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status));

        var sandbox = await db.Sandboxes
            .FirstOrDefaultAsync(x => x.Id == sandboxId, cancellationToken);

        if (sandbox is null)
            return false;

        sandbox.Status = status;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> SetMaxUsersAsync(AccessContext context, Guid sandboxId, int maxUsers, CancellationToken cancellationToken = default)
    {
        await adminGuard.EnsureAdminAsync(context, cancellationToken);

        if (maxUsers < 1)
            throw new ArgumentOutOfRangeException(nameof(maxUsers));

        var settings = await db.SandboxSettings
            .FirstOrDefaultAsync(x => x.SandboxId == sandboxId, cancellationToken);

        if (settings is null)
            return false;

        var currentCount = await db.SandboxMembers
            .CountAsync(x => x.SandboxId == sandboxId, cancellationToken);

        if (maxUsers < currentCount)
            throw new InvalidOperationException("Maximum users cannot be lower than the current member count.");

        settings.MaxUsers = maxUsers;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RenameAsync(AccessContext context, Guid sandboxId, string name, CancellationToken cancellationToken = default)
    {
        await adminGuard.EnsureAdminAsync(context, cancellationToken);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Sandbox name is required.", nameof(name));

        name = name.Trim();

        if (name.Length > 150)
            throw new ArgumentException("Sandbox name cannot exceed 150 characters.", nameof(name));

        var sandbox = await db.Sandboxes
            .FirstOrDefaultAsync(x => x.Id == sandboxId, cancellationToken);

        if (sandbox is null)
            return false;

        if (sandbox.Name == name)
            return true;

        sandbox.Name = name;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<SandboxSummary?> GetAsync(AccessContext context, Guid sandboxId, CancellationToken cancellationToken = default)
    {
        await adminGuard.EnsureAdminAsync(context, cancellationToken);

        return await db.Sandboxes
            .AsNoTracking()
            .Where(x => x.Id == sandboxId)
            .Select(x => new SandboxSummary(
                x.Id,
                x.Name,
                x.OwnerUserKey,
                x.Status,
                x.Members.Count,
                x.Settings.MaxUsers,
                x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<SandboxSummary>> QueryAsync(AccessContext context, SandboxQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await adminGuard.EnsureAdminAsync(context, cancellationToken);

        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Clamp(query.Page, 1, int.MaxValue / pageSize);

        var sandboxes = db.Sandboxes.AsNoTracking();

        if (query.Status is { } status)
            sandboxes = sandboxes.Where(x => x.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            sandboxes = sandboxes.Where(x =>
                EF.Functions.ILike(x.Name, $"%{EscapeLike(search)}%", "\\"));
        }

        var totalCount = await sandboxes.CountAsync(cancellationToken);

        var sortBy = query.SortBy?.Trim().ToLowerInvariant() switch
        {
            "name" => "name",
            "status" => "status",
            "members" => "members",
            "createdat" => "createdat",
            _ => "createdat"
        };

        var ordered = (sortBy, query.Descending) switch
        {
            ("name", false) => sandboxes.OrderBy(x => x.Name),
            ("name", true) => sandboxes.OrderByDescending(x => x.Name),

            ("status", false) => sandboxes.OrderBy(x => x.Status),
            ("status", true) => sandboxes.OrderByDescending(x => x.Status),

            ("members", false) => sandboxes.OrderBy(x => x.Members.Count),
            ("members", true) => sandboxes.OrderByDescending(x => x.Members.Count),

            ("createdat", false) => sandboxes.OrderBy(x => x.CreatedAt),
            _ => sandboxes.OrderByDescending(x => x.CreatedAt)
        };

        var items = await ordered
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SandboxSummary(
                x.Id,
                x.Name,
                x.OwnerUserKey,
                x.Status,
                x.Members.Count,
                x.Settings.MaxUsers,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<SandboxSummary>(items, totalCount, page, pageSize, sortBy, query.Descending);
    }

    public async Task<SandboxStatistics> GetStatisticsAsync(AccessContext context, CancellationToken cancellationToken = default)
    {
        await adminGuard.EnsureAdminAsync(context, cancellationToken);

        var counts = await db.Sandboxes
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var totalMembers = await db.SandboxMembers.AsNoTracking().CountAsync(cancellationToken);

        return new SandboxStatistics(
            Total: counts.Sum(x => x.Count),
            Active: counts
                .Where(x => x.Status == SandboxStatus.Active)
                .Sum(x => x.Count),
            Passive: counts
                .Where(x => x.Status == SandboxStatus.Passive)
                .Sum(x => x.Count),
            Suspended: counts
                .Where(x => x.Status == SandboxStatus.Suspended)
                .Sum(x => x.Count),
            TotalMembers: totalMembers);
    }

    private static string EscapeLike(string value)
        => value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
}
