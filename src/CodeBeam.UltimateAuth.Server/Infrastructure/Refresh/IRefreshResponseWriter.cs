using CodeBeam.UltimateAuth.Server.Infrastructure.Internal;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface IRefreshResponseWriter
    {
        void Write(HttpContext context, RefreshOutcome outcome);
    }
}
