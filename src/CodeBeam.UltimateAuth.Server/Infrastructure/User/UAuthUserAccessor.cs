using System.Security.Claims;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UAuthUserAccessor<TUserId> : IUserAccessor<TUserId>
{
    private readonly ISessionStoreFactory _kernelFactory;
    private readonly IUserIdConverter<TUserId> _userIdConverter;
    private readonly IApplicationProfileReader _clientProfileReader;
    private readonly IEffectiveServerOptionsProvider _effectiveOptions;

    public UAuthUserAccessor(
        ISessionStoreFactory kernelFactory,
        IUserIdConverterResolver converterResolver,
        IApplicationProfileReader clientProfileReader,
        IEffectiveServerOptionsProvider effectiveOptions)
    {
        _kernelFactory = kernelFactory;
        _userIdConverter = converterResolver.GetConverter<TUserId>();
        _clientProfileReader = clientProfileReader;
        _effectiveOptions = effectiveOptions;
    }

    public async Task ResolveAsync(HttpContext context)
    {
        var tenant = context.GetTenant();
        var profile = await _clientProfileReader.ReadAsync(context);
        var mode = _effectiveOptions.GetEffective(context, profile).Mode;

        if (mode == UAuthMode.PureJwt)
        {
            await ResolveJwtAsync(context);
            return;
        }

        await ResolveSessionAsync(context);
    }

    private async Task ResolveJwtAsync(HttpContext context)
    {
        // Authenticate through the registered UltimateAuth scheme.
        // Never trust an arbitrary HttpContext.User principal.
        var result = await context.AuthenticateAsync(UAuthConstants.SchemeDefaults.GlobalScheme);

        if (!result.Succeeded || result.Principal is null)
        {
            SetAnonymous(context);
            return;
        }

        var identities = result.Principal.Identities
            .Where(x => x.IsAuthenticated && x.AuthenticationType == UAuthConstants.SchemeDefaults.GlobalScheme)
            .ToArray();

        if (identities.Length != 1)
        {
            SetAnonymous(context);
            return;
        }

        var subjects = identities[0]
            .FindAll(ClaimTypes.NameIdentifier)
            .Select(x => x.Value)
            .ToArray();

        if (subjects.Length != 1 || string.IsNullOrWhiteSpace(subjects[0]))
        {
            SetAnonymous(context);
            return;
        }

        var userId = _userIdConverter.FromString(subjects[0]);

        context.Items[UAuthConstants.HttpItems.UserContextKey] = AuthUserSnapshot<TUserId>.Authenticated(userId);

        context.User = result.Principal;
    }

    private async Task ResolveSessionAsync(HttpContext context)
    {
        var sessionCtx = context.GetSessionContext();

        if (sessionCtx.IsAnonymous || sessionCtx.SessionId is null)
        {
            SetAnonymous(context);
            return;
        }

        if (sessionCtx.Tenant is not TenantKey tenant)
            throw new InvalidOperationException("Tenant context is missing.");

        var kernel = _kernelFactory.Create(tenant);
        var session = await kernel.GetSessionAsync(sessionCtx.SessionId.Value);

        if (session is null || session.IsRevoked)
        {
            SetAnonymous(context);
            return;
        }

        var userId = _userIdConverter.FromString(session.UserKey.Value);

        context.Items[UAuthConstants.HttpItems.UserContextKey] = AuthUserSnapshot<TUserId>.Authenticated(userId);
    }

    private static void SetAnonymous(HttpContext context)
    {
        context.Items[UAuthConstants.HttpItems.UserContextKey] = AuthUserSnapshot<TUserId>.Anonymous();
    }
}
