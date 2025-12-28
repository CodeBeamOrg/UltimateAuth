using CodeBeam.UltimateAuth.Core.Contracts;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure.Session
{
    public sealed class DefaultSessionContextAccessor : ISessionContextAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultSessionContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public SessionContext? Current
        {
            get
            {
                var ctx = _httpContextAccessor.HttpContext;
                if (ctx is null)
                    return null;

                if (ctx.Items.TryGetValue(SessionContextItemKeys.SessionContext, out var value))
                    return value as SessionContext;

                return null;
            }
        }
    }
}
