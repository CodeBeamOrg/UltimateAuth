using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class SessionContextAccessor : ISessionContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionContextAccessor(IHttpContextAccessor httpContextAccessor)
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

            if (ctx.Items.TryGetValue(UAuthConstants.HttpItems.SessionContext, out var value))
                return value as SessionContext;

            return null;
        }
    }
}
