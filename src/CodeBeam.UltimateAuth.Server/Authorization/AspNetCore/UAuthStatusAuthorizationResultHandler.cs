using System.Security.Claims;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Authorization;

internal sealed class UAuthStatusAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        // Preserve the framework's normal challenge/forbid behavior.
        if (!authorizeResult.Succeeded)
        {
            await _default.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        // The user must have a validated UltimateAuth identity.
        var identity = context.User.Identities.FirstOrDefault(x =>
            x.IsAuthenticated &&
            x.AuthenticationType == UAuthConstants.SchemeDefaults.GlobalScheme);

        if (identity is null ||
            !UserKey.TryParse(
                identity.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                null,
                out var userKey))
        {
            await ForbidAsync(next, context, policy);
            return;
        }

        var tenant = context.GetTenant();
        var runtime = context.RequestServices
            .GetRequiredService<IUAuthRuntimeMarker>();

        bool isActive;
        bool mayUseInactiveException;

        if (runtime is ResourceRuntimeMarker)
        {
            // Read only the trusted result of remote Hub validation.
            var validation = context.Items[
                UAuthConstants.HttpItems.SessionValidationResult
            ] as SessionValidationResult;

            if (validation is null ||
                !validation.IsValid ||
                validation.Tenant != tenant ||
                validation.UserKey != userKey)
            {
                await ForbidAsync(next, context, policy);
                return;
            }

            isActive = validation.UserStatus == UserStatus.Active;

            mayUseInactiveException =
                validation.UserStatus is
                    UserStatus.Suspended or
                    UserStatus.SelfSuspended;
        }
        else
        {
            var provider = context.RequestServices
                .GetRequiredService<IUserRuntimeStateProvider>();

            var state = await provider.GetAsync(
                tenant, userKey, context.RequestAborted);

            if (state is null ||
                !state.Exists ||
                state.IsDeleted ||
                !state.CanAuthenticate)
            {
                await ForbidAsync(next, context, policy);
                return;
            }

            isActive = state.IsActive;

            mayUseInactiveException = !state.IsActive;
        }

        var allowInactive = context.GetEndpoint()?
            .Metadata.GetMetadata<UAuthAllowInactiveAttribute>() is not null;

        if (!isActive &&
            !(allowInactive && mayUseInactiveException))
        {
            await ForbidAsync(next, context, policy);
            return;
        }

        await _default.HandleAsync(
            next, context, policy, authorizeResult);
    }

    private Task ForbidAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy)
        => _default.HandleAsync(
            next,
            context,
            policy,
            PolicyAuthorizationResult.Forbid());
}
