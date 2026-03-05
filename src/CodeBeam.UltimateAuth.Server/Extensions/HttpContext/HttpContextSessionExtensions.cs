using CodeBeam.UltimateAuth.Core.Constants;
using CodeBeam.UltimateAuth.Core.Contracts;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class HttpContextSessionExtensions
{
    public static SessionContext GetSessionContext(this HttpContext context)
    {
        if (context.Items.TryGetValue(UAuthConstants.HttpItems.SessionContext, out var value) && value is SessionContext session)
        {
            return session;
        }

        return SessionContext.Anonymous();
    }
}
