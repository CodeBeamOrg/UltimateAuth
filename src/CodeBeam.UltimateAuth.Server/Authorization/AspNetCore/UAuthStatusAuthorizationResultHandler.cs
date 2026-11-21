using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.ResourceApi;
using CodeBeam.UltimateAuth.Server.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.Authorization;

internal sealed class UAuthStatusAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Succeeded)
        {
            await _default.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

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

        if (runtime is ResourceRuntimeMarker)
        {
            var mode = context.RequestServices.GetRequiredService<ResourceApiAuthModeProvider>().Mode;

            if (mode == UAuthMode.PureJwt)
            {
                await _default.HandleAsync(next, context, policy, authorizeResult);

                return;
            }
        }
        else
        {
            var profileReader = context.RequestServices.GetRequiredService<IApplicationProfileReader>();
            var optionsProvider = context.RequestServices
                .GetRequiredService<IEffectiveServerOptionsProvider>();

            var applicationProfile = await profileReader.ReadAsync(context);
            var effective = optionsProvider.GetEffective(context, applicationProfile);

            if (effective.Mode == UAuthMode.PureJwt)
            {
                await _default.HandleAsync(next, context, policy, authorizeResult);

                return;
            }
        }

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
