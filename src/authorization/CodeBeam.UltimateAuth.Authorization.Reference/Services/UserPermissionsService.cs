using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class UserPermissionService : IUserPermissionService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly IUserPermissionStore _permissions;

    public UserPermissionService(IAccessOrchestrator accessOrchestrator, IUserPermissionStore permissions)
    {
        _accessOrchestrator = accessOrchestrator;
        _permissions = permissions;
    }

    public async Task<UserPermissionsResult> GetMyPermissionsAsync(
        AccessContext context,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!context.IsAuthenticated ||
            context.ActorUserKey is null ||
            context.TargetUserKey != context.ActorUserKey ||
            context.Action != UAuthActions.Authorization.Permissions.GetSelf)
        {
            throw new UAuthAuthorizationException(
                "invalid_permission_self_context");
        }

        var command = new AccessCommand<UserPermissionsResult>(
            async innerCt =>
            {
                var permissions = await _permissions.GetPermissionsAsync(
                    context.ResourceTenant,
                    context.ActorUserKey.Value,
                    innerCt);

                return new UserPermissionsResult
                {
                    Permissions = permissions
                        .Distinct()
                        .OrderBy(x => x.Value, StringComparer.Ordinal)
                        .ToArray()
                };
            });

        return await _accessOrchestrator.ExecuteAsync(
            context,
            command,
            ct);
    }
}
