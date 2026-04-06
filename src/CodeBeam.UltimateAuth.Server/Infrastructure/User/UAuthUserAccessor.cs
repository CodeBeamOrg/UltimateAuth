using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UAuthUserAccessor<TUserId> : IUserAccessor<TUserId>
{
    private readonly ISessionStoreFactory _kernelFactory;
    private readonly IUserIdConverter<TUserId> _userIdConverter;

    public UAuthUserAccessor(ISessionStoreFactory kernelFactory, IUserIdConverterResolver converterResolver)
    {
        _kernelFactory = kernelFactory;
        _userIdConverter = converterResolver.GetConverter<TUserId>();
    }

    public async Task ResolveAsync(HttpContext context)
    {
        var sessionCtx = context.GetSessionContext();

        if (sessionCtx.IsAnonymous || sessionCtx.SessionId is null)
        {
            context.Items[UAuthConstants.HttpItems.UserContextKey] = AuthUserSnapshot<TUserId>.Anonymous();
            return;
        }

        if (sessionCtx.Tenant is not TenantKey tenant)
        {
            throw new InvalidOperationException("Tenant context is missing.");
        }

        var kernel = _kernelFactory.Create(tenant);
        var session = await kernel.GetSessionAsync(sessionCtx.SessionId.Value);

        if (session is null || session.IsRevoked)
        {
            context.Items[UAuthConstants.HttpItems.UserContextKey] = AuthUserSnapshot<TUserId>.Anonymous();
            return;
        }

        var userId = _userIdConverter.FromString(session.UserKey.Value);
        context.Items[UAuthConstants.HttpItems.UserContextKey] = AuthUserSnapshot<TUserId>.Authenticated(userId);
    }
}
