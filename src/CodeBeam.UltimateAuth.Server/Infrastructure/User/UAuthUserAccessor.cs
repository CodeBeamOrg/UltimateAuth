using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Middlewares;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class UAuthUserAccessor<TUserId> : IUserAccessor<TUserId>
    {
        private readonly ISessionStore _sessionStore;
        private readonly IUserIdConverter<TUserId> _userIdConverter;

        public UAuthUserAccessor(
            ISessionStore sessionStore,
            IUserIdConverterResolver converterResolver)
        {
            _sessionStore = sessionStore;
            _userIdConverter = converterResolver.GetConverter<TUserId>();
        }

        public async Task ResolveAsync(HttpContext context)
        {
            var sessionCtx = context.GetSessionContext();

            if (sessionCtx.IsAnonymous)
            {
                context.Items[UserMiddleware.UserContextKey] = AuthUserSnapshot<TUserId>.Anonymous();
                return;
            }

            var session = await _sessionStore.GetSessionAsync(sessionCtx.TenantId, sessionCtx.SessionId!.Value);

            if (session is null || session.IsRevoked)
            {
                context.Items[UserMiddleware.UserContextKey] = AuthUserSnapshot<TUserId>.Anonymous();
                return;
            }

            var userId = _userIdConverter.FromString(session.UserKey.Value);
            context.Items[UserMiddleware.UserContextKey] = AuthUserSnapshot<TUserId>.Authenticated(userId);
        }

    }
}
