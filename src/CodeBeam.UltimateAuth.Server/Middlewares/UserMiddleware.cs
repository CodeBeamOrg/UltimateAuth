using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Middlewares
{
    public sealed class UserMiddleware
    {
        private readonly RequestDelegate _next;

        public const string UserContextKey = "__UAuthUser";

        public UserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userAccessor = context.RequestServices.GetRequiredService<IUserAccessor>();
            await userAccessor.ResolveAsync(context);
            await _next(context);
        }
    }
}
