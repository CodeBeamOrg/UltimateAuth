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
    private readonly IUserRoleStoreFactory _userRoleFactory;
    private readonly IRoleStoreFactory _roleFactory;
    private readonly IClock _clock;

    public UserRoleService(IAccessOrchestrator accessOrchestrator, IUserRoleStoreFactory userRoleFactory, IRoleStoreFactory roleFactory, IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _userRoleFactory = userRoleFactory;
        _roleFactory = roleFactory;
        _clock = clock;
    }

    public async Task AssignAsync(AccessContext context, UserKey targetUserKey, string roleName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var now = _clock.UtcNow;

        var cmd = new AccessCommand(async innerCt =>
        {
            var roleStore = _roleFactory.Create(context.ResourceTenant);
            var userRoleStore = _userRoleFactory.Create(context.ResourceTenant);
            var normalized = roleName.Trim().ToUpperInvariant();
            var role = await roleStore.GetByNameAsync(normalized, innerCt);

            if (role is null || role.IsDeleted)
                throw new UAuthNotFoundException("role_not_found");

            await userRoleStore.AssignAsync(targetUserKey, role.Id, now, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task RemoveAsync(AccessContext context, UserKey targetUserKey, string roleName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand(async innerCt =>
        {
            var roleStore = _roleFactory.Create(context.ResourceTenant);
            var userRoleStore = _userRoleFactory.Create(context.ResourceTenant);
            var normalized = roleName.Trim().ToUpperInvariant();
            var role = await roleStore.GetByNameAsync(normalized, innerCt);

            if (role is null)
                return;

            await userRoleStore.RemoveAsync(targetUserKey, role.Id, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<PagedResult<UserRoleInfo>> GetRolesAsync(AccessContext context, UserKey targetUserKey, PageRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<PagedResult<UserRoleInfo>>(async innerCt =>
        {
            request = request.Normalize();

            var roleStore = _roleFactory.Create(context.ResourceTenant);
            var userRoleStore = _userRoleFactory.Create(context.ResourceTenant);
            var assignments = await userRoleStore.GetAssignmentsAsync(targetUserKey, innerCt);
            var roleIds = assignments.Select(x => x.RoleId).ToArray();
            var roles = await roleStore.GetByIdsAsync(roleIds, innerCt);

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
