using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal sealed class ResourceUserAccessor<TUserId> : IUserAccessor<TUserId>
{
    private readonly IUserIdConverter<TUserId> _converter;

    public ResourceUserAccessor(IUserIdConverterResolver resolver)
    {
        _converter = resolver.GetConverter<TUserId>();
    }

    public Task ResolveAsync(HttpContext context)
    {
        var result = context.Items[UAuthConstants.HttpItems.SessionValidationResult] as SessionValidationResult;

        if (result is null || !result.IsValid || result.UserKey is null)
        {
            context.Items[UAuthConstants.HttpItems.UserContextKey] = AuthUserSnapshot<TUserId>.Anonymous();
            return Task.CompletedTask;
        }

        var userId = _converter.FromString(result.UserKey.Value);
        context.Items[UAuthConstants.HttpItems.UserContextKey] = AuthUserSnapshot<TUserId>.Authenticated(userId);

        return Task.CompletedTask;
    }
}
