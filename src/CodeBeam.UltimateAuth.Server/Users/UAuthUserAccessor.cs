using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contexts;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Middlewares;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Users
{
    public sealed class UAuthUserAccessor<TUserId> : IUserAccessor
    {
        private readonly ISessionStore<TUserId> _sessionStore;
        private readonly IUserStore<TUserId> _userStore;

        public UAuthUserAccessor(
            ISessionStore<TUserId> sessionStore,
            IUserStore<TUserId> userStore)
        {
            _sessionStore = sessionStore;
            _userStore = userStore;
        }

        public async Task ResolveAsync(HttpContext context)
        {
            var sessionCtx = context.GetSessionContext();

            if (sessionCtx.IsAnonymous)
            {
                context.Items[UserMiddleware.UserContextKey] =
                    UserContext<TUserId>.Anonymous();
                return;
            }

            // 🔐 Load & validate session
            var session = await _sessionStore.GetSessionAsync(
                sessionCtx.TenantId,
                sessionCtx.SessionId!.Value);

            if (session is null || session.IsRevoked)
            {
                context.Items[UserMiddleware.UserContextKey] =
                    UserContext<TUserId>.Anonymous();
                return;
            }

            // 👤 Load user
            var user = await _userStore.FindByIdAsync(session.UserId);

            if (user is null)
            {
                context.Items[UserMiddleware.UserContextKey] =
                    UserContext<TUserId>.Anonymous();
                return;
            }

            context.Items[UserMiddleware.UserContextKey] =
                new UserContext<TUserId>
                {
                    UserId = session.UserId,
                    User = user
                };
        }

    }
}
