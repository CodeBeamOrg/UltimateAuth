using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class UserRoleService : IUserRoleService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly IUserRoleStore _userRoles;
    private readonly IRoleStore _roles;

    public UserRoleService(IAccessOrchestrator accessOrchestrator, IUserRoleStore userRoles, IRoleStore roles)
    {
        _accessOrchestrator = accessOrchestrator;
        _userRoles = userRoles;
        _roles = roles;
    }

    public async Task AssignAsync(AccessContext context, UserKey targetUserKey, string roleName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand(async innerCt =>
        {
            var normalized = roleName.Trim().ToUpperInvariant();
            var role = await _roles.GetByNameAsync(context.ResourceTenant, normalized, innerCt);

            if (role is null || role.IsDeleted)
                throw new InvalidOperationException("role_not_found");

            await _userRoles.AssignAsync(context.ResourceTenant, targetUserKey, role.Id, innerCt);
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

    public async Task<IReadOnlyCollection<string>> GetRolesAsync(AccessContext context, UserKey targetUserKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<IReadOnlyCollection<string>>(async innerCt =>
        {
            var roleIds = await _userRoles.GetRolesAsync(context.ResourceTenant, targetUserKey, innerCt);
            var roles = await _roles.GetByIdsAsync(context.ResourceTenant, roleIds, innerCt);

            return roles.Select(r => r.Name).ToArray();
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }
}
