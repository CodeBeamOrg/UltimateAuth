using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class RoleService : IRoleService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly IRoleStore _roles;
    private readonly IClock _clock;

    public RoleService(IAccessOrchestrator accessOrchestrator, IRoleStore roles, IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _roles = roles;
        _clock = clock;
    }

    public async Task<Role> CreateAsync(AccessContext context, string name, IEnumerable<Permission>? permissions, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<Role>(async innerCt =>
        {
            var role = Role.Create(RoleId.New(), context.ResourceTenant, name, permissions, _clock.UtcNow);
            await _roles.AddAsync(role, innerCt);

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
            var role = await _roles.GetAsync(key, innerCt);

            if (role is null || role.IsDeleted)
                throw new UAuthNotFoundException("role_not_found");

            var expected = role.Version;
            role.Rename(newName, _clock.UtcNow);

            await _roles.SaveAsync(role, expected, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task DeleteAsync(AccessContext context, RoleId roleId, DeleteMode mode, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand(async innerCt =>
        {
            var key = new RoleKey(context.ResourceTenant, roleId);
            var role = await _roles.GetAsync(key, innerCt);

            if (role is null)
                throw new UAuthNotFoundException("role_not_found");

            await _roles.DeleteAsync(key, role.Version, mode, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task SetPermissionsAsync(AccessContext context, RoleId roleId, IEnumerable<Permission> permissions, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand(async innerCt =>
        {
            var key = new RoleKey(context.ResourceTenant, roleId);
            var role = await _roles.GetAsync(key, innerCt);

            if (role is null || role.IsDeleted)
                throw new UAuthNotFoundException("role_not_found");

            var expected = role.Version;
            role.SetPermissions(permissions, _clock.UtcNow);

            await _roles.SaveAsync(role, expected, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<PagedResult<Role>> QueryAsync(AccessContext context, RoleQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<PagedResult<Role>>(async innerCt =>
        {
            return await _roles.QueryAsync(context.ResourceTenant, query, innerCt);
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }
}
