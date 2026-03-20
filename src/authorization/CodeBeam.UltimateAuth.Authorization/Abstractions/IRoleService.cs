using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IRoleService
{
    Task<Role> CreateAsync(AccessContext context, string name, IEnumerable<Permission>? permissions, CancellationToken ct = default);

    Task RenameAsync(AccessContext context, RoleId roleId, string newName, CancellationToken ct = default);

    Task<DeleteRoleResult> DeleteAsync(AccessContext context, RoleId roleId, DeleteMode mode, CancellationToken ct = default);

    Task SetPermissionsAsync(AccessContext context, RoleId roleId, IEnumerable<Permission> permissions, CancellationToken ct = default);

    Task<PagedResult<Role>> QueryAsync(AccessContext context, RoleQuery query, CancellationToken ct = default);
}
