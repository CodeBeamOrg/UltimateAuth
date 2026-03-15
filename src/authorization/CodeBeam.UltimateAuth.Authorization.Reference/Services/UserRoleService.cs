using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class UserRoleService : IUserRoleService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly IUserRoleStore _userRoles;
    private readonly IRoleStore _roles;
    private readonly IClock _clock;

    public UserRoleService(IAccessOrchestrator accessOrchestrator, IUserRoleStore userRoles, IRoleStore roles, IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _userRoles = userRoles;
        _roles = roles;
        _clock = clock;
    }

    public async Task AssignAsync(AccessContext context, UserKey targetUserKey, string roleName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var now = _clock.UtcNow;

        var cmd = new AccessCommand(async innerCt =>
        {
            var normalized = roleName.Trim().ToUpperInvariant();
            var role = await _roles.GetByNameAsync(context.ResourceTenant, normalized, innerCt);

            if (role is null || role.IsDeleted)
                throw new UAuthNotFoundException("role_not_found");

            await _userRoles.AssignAsync(context.ResourceTenant, targetUserKey, role.Id, now, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task RemoveAsync(AccessContext context, UserKey targetUserKey, string roleName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand(async innerCt =>
        {
            var normalized = roleName.Trim().ToUpperInvariant();
            var role = await _roles.GetByNameAsync(context.ResourceTenant, normalized, innerCt);

            if (role is null)
                return;

            await _userRoles.RemoveAsync(context.ResourceTenant, targetUserKey, role.Id, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<PagedResult<UserRoleInfo>> GetRolesAsync(AccessContext context, UserKey targetUserKey, PageRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<PagedResult<UserRoleInfo>>(async innerCt =>
        {
            request = request.Normalize();

            var assignments = await _userRoles.GetAssignmentsAsync(context.ResourceTenant, targetUserKey, innerCt);
            var roleIds = assignments.Select(x => x.RoleId).ToArray();
            var roles = await _roles.GetByIdsAsync(context.ResourceTenant, roleIds, innerCt);

            var roleMap = roles.ToDictionary(x => x.Id);

            var joined = assignments
                .Where(a => roleMap.ContainsKey(a.RoleId))
                .Select(a => new UserRoleInfo
                {
                    Tenant = a.Tenant,
                    UserKey = a.UserKey,
                    RoleId = a.RoleId,
                    AssignedAt = a.AssignedAt,
                    Name = roleMap[a.RoleId].Name
                })
                .ToList();

            var total = joined.Count;

            var pageItems = joined.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();

            return new PagedResult<UserRoleInfo>(
                pageItems,
                total,
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.Descending);
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }
}
