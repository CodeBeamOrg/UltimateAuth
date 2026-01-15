using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Middlewares;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class HttpContextUserExtensions
    {
        public static AuthUserSnapshot<UserKey> GetUserContext(this HttpContext ctx)
        {
            if (ctx.Items.TryGetValue(UserMiddleware.UserContextKey, out var value) && value is AuthUserSnapshot<UserKey> user)
            {
                return user;
            }

            return AuthUserSnapshot<UserKey>.Anonymous();
        }
    }
}
