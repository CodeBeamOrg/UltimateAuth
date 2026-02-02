using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Middlewares;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UAuthUserAccessor<TUserId> : IUserAccessor<TUserId>
{
    private readonly ISessionStoreKernelFactory _kernelFactory;
    private readonly IUserIdConverter<TUserId> _userIdConverter;

    public UAuthUserAccessor(ISessionStoreKernelFactory kernelFactory, IUserIdConverterResolver converterResolver)
    {
        _kernelFactory = kernelFactory;
        _userIdConverter = converterResolver.GetConverter<TUserId>();
    }

    public async Task ResolveAsync(HttpContext context)
    {
        var sessionCtx = context.GetSessionContext();

        if (sessionCtx.IsAnonymous || sessionCtx.SessionId is null)
        {
            context.Items[UserMiddleware.UserContextKey] = AuthUserSnapshot<TUserId>.Anonymous();
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
            context.Items[UserMiddleware.UserContextKey] = AuthUserSnapshot<TUserId>.Anonymous();
            return;
        }

        var userId = _userIdConverter.FromString(session.UserKey.Value);
        context.Items[UserMiddleware.UserContextKey] = AuthUserSnapshot<TUserId>.Authenticated(userId);
    }
}
