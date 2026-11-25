using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal sealed class ResourceUserAccessor<TUserId> : IUserAccessor<TUserId>
{
    private readonly IUserIdConverter<TUserId> _converter;

    public ResourceUserAccessor(IUserIdConverterResolver resolver)
    {
        _converter = resolver.GetConverter<TUserId>();
    }

    public async Task ResolveAsync(HttpContext context)
    {
        await ResolveAuthenticationAsync(context);
    }

    private async Task ResolveAuthenticationAsync(HttpContext context)
    {
        var result = await context.AuthenticateAsync(UAuthConstants.SchemeDefaults.GlobalScheme);

        if (!result.Succeeded || result.Principal is null)
        {
            SetAnonymous(context);
            return;
        }

        var identities = result.Principal.Identities
            .Where(x =>
                x.IsAuthenticated &&
                x.AuthenticationType == UAuthConstants.SchemeDefaults.GlobalScheme)
            .ToArray();

        if (identities.Length != 1 ||
            result.Principal.Identities.Count() != 1)
        {
            SetAnonymous(context);
            return;
        }

        var subjects = identities[0]
            .FindAll(ClaimTypes.NameIdentifier)
            .Select(x => x.Value)
            .ToArray();

        if (subjects.Length != 1 ||
            string.IsNullOrWhiteSpace(subjects[0]))
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

        context.Items[UAuthConstants.HttpItems.UserContextKey] =
            AuthUserSnapshot<TUserId>.Authenticated(userId);

        context.User = result.Principal;
    }

    private static void SetAnonymous(HttpContext context)
    {
        context.Items[UAuthConstants.HttpItems.UserContextKey] = AuthUserSnapshot<TUserId>.Anonymous();
    }
}
