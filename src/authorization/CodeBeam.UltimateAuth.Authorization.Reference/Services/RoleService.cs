using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class RoleService : IRoleService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly IRoleStoreFactory _roleFactory;
    private readonly IUserRoleStoreFactory _userRoleFactory;
    private readonly IClock _clock;

    public RoleService(
        IAccessOrchestrator accessOrchestrator,
        IRoleStoreFactory roleFactory,
        IUserRoleStoreFactory userRoleFactory,
        IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _roleFactory = roleFactory;
        _userRoleFactory = userRoleFactory;
        _clock = clock;
    }

    public async Task<Role> CreateAsync(AccessContext context, string name, IEnumerable<Permission>? permissions, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<Role>(async innerCt =>
        {
            var role = Role.Create(RoleId.New(), context.ResourceTenant, name, permissions, _clock.UtcNow);
            var roleStore = _roleFactory.Create(context.ResourceTenant);
            await roleStore.AddAsync(role, innerCt);

            return role;
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task RenameAsync(AccessContext context, RoleId roleId, string newName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand(async innerCt =>
        {
            var key = new RoleKey(context.ResourceTenant, roleId);
            var roleStore = _roleFactory.Create(context.ResourceTenant);
            var role = await roleStore.GetAsync(key, innerCt);

            if (role is null || role.IsDeleted)
                throw new UAuthNotFoundException("role_not_found");

            var expected = role.Version;
            role.Rename(newName, _clock.UtcNow);

            await roleStore.SaveAsync(role, expected, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<DeleteRoleResult> DeleteAsync(AccessContext context, RoleId roleId, DeleteMode mode, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<DeleteRoleResult>(async innerCt =>
        {
            var key = new RoleKey(context.ResourceTenant, roleId);
            var roleStore = _roleFactory.Create(context.ResourceTenant);
            var role = await roleStore.GetAsync(key, innerCt);

            if (role is null)
                throw new UAuthNotFoundException("role_not_found");

            var userRoleStore = _userRoleFactory.Create(context.ResourceTenant);
            var removed = await userRoleStore.CountAssignmentsAsync(roleId, innerCt);
            await userRoleStore.RemoveAssignmentsByRoleAsync(roleId, innerCt);
            await roleStore.DeleteAsync(key, role.Version, mode, _clock.UtcNow, innerCt);

            return new DeleteRoleResult
            {
                RoleId = roleId,
                RemovedAssignments = removed,
                Mode = mode,
                DeletedAt = _clock.UtcNow
            };
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task SetPermissionsAsync(AccessContext context, RoleId roleId, IEnumerable<Permission> permissions, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand(async innerCt =>
        {
            var roleStore = _roleFactory.Create(context.ResourceTenant);
            var key = new RoleKey(context.ResourceTenant, roleId);
            var role = await roleStore.GetAsync(key, innerCt);

            if (role is null || role.IsDeleted)
                throw new UAuthNotFoundException("role_not_found");

            var expected = role.Version;
            role.SetPermissions(permissions, _clock.UtcNow);

            await roleStore.SaveAsync(role, expected, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<PagedResult<Role>> QueryAsync(AccessContext context, RoleQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<PagedResult<Role>>(async innerCt =>
        {
            var roleStore = _roleFactory.Create(context.ResourceTenant);
            return await roleStore.QueryAsync(query, innerCt);
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }
}
