using CodeBeam.UltimateAuth.Server.Users;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Middlewares
{
    public sealed class UserMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IUserAccessor _userAccessor;

        public const string UserContextKey = "__UAuthUser";

        public UserMiddleware(
            RequestDelegate next,
            IUserAccessor userAccessor)
        {
            _next = next;
            _userAccessor = userAccessor;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _userAccessor.ResolveAsync(context);
            await _next(context);
        }
    }
}
