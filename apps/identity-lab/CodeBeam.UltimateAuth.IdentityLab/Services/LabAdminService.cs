using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabAdminService(IDbContextFactory<LabDbContext> dbFactory, IRoleStoreFactory roleStoreFactory, 
                                    IUserRoleStoreFactory userRoleStoreFactory, IUAuthClient uAuthClient)
{
    public async Task<bool> IsAdminAsync(UserKey actorUserKey, CancellationToken ct = default)
    {
        var tenant = TenantKeys.Single;

        var assignments = await userRoleStoreFactory.Create(tenant).GetAssignmentsAsync(actorUserKey, ct);

        if (assignments.Count == 0)
            return false;

        var roleIds = assignments
            .Select(x => x.RoleId)
            .Distinct()
            .ToArray();

        var roles = await roleStoreFactory
            .Create(tenant)
            .GetByIdsAsync(roleIds, ct);

        return roles.Any(x =>
            string.Equals(
                x.Name,
                "Ladmin",
                StringComparison.OrdinalIgnoreCase));
    }

    private async Task RequireAdminAsync(UserKey actorUserKey, CancellationToken ct)
    {
        if (!await IsAdminAsync(actorUserKey, ct))
            throw new UnauthorizedAccessException("Platform administrator access is required.");
    }

    public async Task<AdminSandboxItem?> GetSandboxAsync(UserKey actorUserKey, Guid sandboxId, CancellationToken ct = default)
    {
        await RequireAdminAsync(actorUserKey, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var sandbox = await db.Sandboxes
            .AsNoTracking()
            .Where(x => x.Id == sandboxId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.OwnerUserKey,
                x.Status,
                MaxUsers = x.Settings.MaxUsers,
                MemberCount = x.Members.Count()
            })
            .FirstOrDefaultAsync(ct);

        if (sandbox is null)
            return null;

        return new AdminSandboxItem(
            sandbox.Id,
            sandbox.Name,
            sandbox.OwnerUserKey,
            sandbox.OwnerUserKey.Value,
            string.Empty,
            sandbox.Status,
            sandbox.MemberCount,
            sandbox.MaxUsers);
    }

    public async Task UpdateSandboxAsync(UserKey actorUserKey, Guid sandboxId, UpdateSandboxRequest request, CancellationToken ct = default)
    {
        await RequireAdminAsync(actorUserKey, ct);

        if (request.MaxUsers is < 1 or > SandboxLimits.AbsoluteMaxUsers)
        {
            throw new ArgumentOutOfRangeException(nameof(request.MaxUsers), $"Sandbox capacity must be between 1 and {SandboxLimits.AbsoluteMaxUsers}.");
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Sandbox name is required.");

        if (name.Length > 120)
            throw new ArgumentException("Sandbox name is too long.");

        if (request.MaxUsers < 1)
            throw new ArgumentException("Maximum users must be at least 1.");

        if (!Enum.IsDefined(request.Status))
            throw new ArgumentException("Invalid sandbox status.");

        await using var db =
            await dbFactory.CreateDbContextAsync(ct);

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var sandbox = await db.Sandboxes
            .Include(x => x.Settings)
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == sandboxId, ct);

        if (sandbox is null)
            throw new InvalidOperationException("Sandbox not found.");

        if (request.MaxUsers < sandbox.Members.Count)
            throw new InvalidOperationException("Maximum users cannot be lower than the current member count.");

        sandbox.Name = name;
        sandbox.Status = request.Status;
        sandbox.Settings.MaxUsers = request.MaxUsers;

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<SandboxMember>> GetMembersAsync(UserKey actorUserKey, Guid sandboxId, CancellationToken ct = default)
    {
        await RequireAdminAsync(actorUserKey, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.SandboxMembers
            .AsNoTracking()
            .Where(x => x.SandboxId == sandboxId)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AdminSandboxMemberItem>> GetMemberDetailsAsync(UserKey actorUserKey, Guid sandboxId, CancellationToken ct = default)
    {
        var members = await GetMembersAsync(actorUserKey, sandboxId, ct);

        var result = new List<AdminSandboxMemberItem>();

        foreach (var member in members)
        {
            var userResult = await uAuthClient.Users.GetUserAsync(
                member.UserKey);

            if (!userResult.IsSuccess || userResult.Value is null)
            {
                throw new InvalidOperationException(
                    $"Unable to load member {member.UserKey.Value}.");
            }

            var user = userResult.Value;

            result.Add(new AdminSandboxMemberItem(
                member.UserKey,
                user.DisplayName ?? user.UserName ?? "Unknown",
                user.PrimaryEmail ?? string.Empty,
                member.Role,
                user.Status,
                member.JoinedAt));
        }

        return result;
    }

    public async Task<AdminSandboxPage> QuerySandboxesAsync(UserKey actorUserKey, AdminSandboxQuery request, CancellationToken ct = default)
    {
        await RequireAdminAsync(actorUserKey, ct);

        var page = Math.Max(0, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var query = db.Sandboxes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{search}%"));
        }

        if (Enum.TryParse<SandboxStatus>(request.Status, true, out var status))
        {
            query = query.Where(x => x.Status == status);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(x => new AdminSandboxItem(
                x.Id,
                x.Name,
                x.OwnerUserKey,
                x.OwnerUserKey.Value,
                string.Empty,
                x.Status,
                x.Members.Count(),
                x.Settings.MaxUsers))
            .ToListAsync(ct);

        return new AdminSandboxPage(items, totalCount, page, pageSize);
    }

    public async Task<AdminDashboardSnapshot> GetStatisticsAsync(UserKey actorUserKey, CancellationToken ct = default)
    {
        await RequireAdminAsync(actorUserKey, ct);

        await using var db =
            await dbFactory.CreateDbContextAsync(ct);

        var total = await db.Sandboxes.CountAsync(ct);

        var active = await db.Sandboxes.CountAsync(
            x => x.Status == SandboxStatus.Active, ct);

        var suspended = await db.Sandboxes.CountAsync(
            x => x.Status == SandboxStatus.Suspended, ct);

        var members = await db.SandboxMembers.CountAsync(ct);

        return new AdminDashboardSnapshot(
            total, active, suspended, members);
    }
}
