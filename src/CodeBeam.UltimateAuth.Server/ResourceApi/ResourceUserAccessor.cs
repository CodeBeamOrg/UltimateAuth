using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal sealed class ResourceUserAccessor<TUserId> : IUserAccessor<TUserId>
{
    private readonly IUserIdConverter<TUserId> _converter;
    private readonly UAuthResourceApiOptions _options;

    public ResourceUserAccessor(IUserIdConverterResolver resolver, IOptions<UAuthResourceApiOptions> options)
    {
        _converter = resolver.GetConverter<TUserId>();
        _options = options.Value;
    }

    public async Task ResolveAsync(HttpContext context)
    {
        if (_options.Auth.Mode == UAuthMode.PureJwt)
        {
            await ResolveJwtAsync(context);
            return;
        }

        ResolveSession(context);
    }

    private async Task ResolveJwtAsync(HttpContext context)
    {
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

        TUserId userId;

        try
        {
            userId = _converter.FromString(subjects[0]);
        }
        catch (FormatException)
        {
            SetAnonymous(context);
            return;
        }

        context.Items[UAuthConstants.HttpItems.UserContextKey] = AuthUserSnapshot<TUserId>.Authenticated(userId);

        context.User = result.Principal;
    }

    private void ResolveSession(HttpContext context)
    {
        var result = context.Items[UAuthConstants.HttpItems.SessionValidationResult] as SessionValidationResult;

        if (result is null || !result.IsValid || result.UserKey is null)
        {
            SetAnonymous(context);
            return;
        }

        var userId = _converter.FromString(result.UserKey.Value);

        context.Items[UAuthConstants.HttpItems.UserContextKey] = AuthUserSnapshot<TUserId>.Authenticated(userId);
    }

    private static void SetAnonymous(HttpContext context)
    {
        context.Items[UAuthConstants.HttpItems.UserContextKey] = AuthUserSnapshot<TUserId>.Anonymous();
    }
}
