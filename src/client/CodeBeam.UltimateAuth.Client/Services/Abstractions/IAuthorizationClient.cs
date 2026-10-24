using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Services;

/// <summary>
/// Provides authorization and role management operations for the current application.
/// </summary>
/// <remarks>
/// <para>
/// This client is responsible for evaluating permissions, managing roles,
/// and assigning authorization policies to users.
/// </para>
///
/// <para>
/// Key capabilities:
/// <list type="bullet">
/// <item><description>Permission checks via <see cref="CheckAsync"/></description></item>
/// <item><description>User role management (assign/remove roles)</description></item>
/// <item><description>Role lifecycle management (create, rename, delete)</description></item>
/// <item><description>Permission assignment to roles</description></item>
/// </list>
/// </para>
///
/// <para>
/// Important:
/// <list type="bullet">
/// <item><description>Authorization decisions are evaluated on the server and may depend on current session context.</description></item>
/// <item><description>Role changes may not take effect immediately for active sessions depending on caching strategy.</description></item>
/// <item><description>Multi-tenant isolation is enforced; all operations are scoped to the current tenant.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IAuthorizationClient
{
    /// <summary>
    /// Evaluates whether the current authenticated user is authorized to perform a specific action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Performs an on-demand, server-side authorization evaluation using the current authentication context and applicable policies.
    /// </para>
    /// <para>
    /// A successful request returns an <see cref="AuthorizationResult"/> even when authorization is denied or reauthentication is required.
    /// The authorization decision is separate from the API request result.
    /// </para>
    /// <para>
    /// This method does not execute the requested operation or grant authorization for subsequent requests.
    /// Protected operations must independently enforce authorization when executed.
    /// </para>
    /// </remarks>
    Task<UAuthResult<AuthorizationResult>> CheckAsync(AuthorizationCheckRequest request);

    /// <summary>
    /// Retrieves roles assigned to the current user.
    /// </summary>
    /// <remarks>
    /// Results may be paginated. Use <paramref name="request"/> to control paging behavior.
    /// </remarks>
    Task<UAuthResult<UserRolesGetResult>> GetMyRolesAsync(PageRequest? request = null);

    /// <summary>
    /// Retrieves roles assigned to a specific user.
    /// </summary>
    /// <remarks>
    /// Requires appropriate administrative permissions.
    /// </remarks>
    Task<UAuthResult<UserRolesGetResult>> GetUserRolesAsync(UserKey userKey, PageRequest? request = null);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <remarks>
    /// The role must exist and the caller must have sufficient privileges.
    /// </remarks>
    Task<UAuthResult> AssignRoleToUserAsync(AssignRoleRequest request);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    Task<UAuthResult> RemoveRoleFromUserAsync(RemoveRoleRequest request);


    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <remarks>
    /// Role names must be unique (case-insensitive) within a tenant.
    /// </remarks>
    Task<UAuthResult<RoleInfo>> CreateRoleAsync(CreateRoleRequest request);

    /// <summary>
    /// Queries roles with filtering and pagination.
    /// </summary>
    Task<UAuthResult<PagedResult<RoleInfo>>> QueryRolesAsync(RoleQuery request);

    /// <summary>
    /// Renames an existing role.
    /// </summary>
    /// <remarks>
    /// May fail if the target name already exists.
    /// </remarks>
    Task<UAuthResult> RenameRoleAsync(RenameRoleRequest request);

    /// <summary>
    /// Sets the permissions associated with a role.
    /// </summary>
    /// <remarks>
    /// This operation replaces existing permissions.
    /// </remarks>
    Task<UAuthResult> SetRolePermissionsAsync(SetRolePermissionsRequest request);

    /// <summary>
    /// Deletes a role.
    /// </summary>
    /// <remarks>
    /// Deleting a role removes it from all users.
    /// </remarks>
    Task<UAuthResult<RoleDeleteResult>> DeleteRoleAsync(DeleteRoleRequest request);
}
