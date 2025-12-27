using CodeBeam.UltimateAuth.Server.Infrastructure.Internal;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class DefaultRefreshResponseWriter : IRefreshResponseWriter
    {
        private readonly UAuthDiagnosticsOptions _diagnostics;

        public DefaultRefreshResponseWriter(IOptions<UAuthServerOptions> options)
        {
            _diagnostics = options.Value.Diagnostics;
        }

        public void Write(HttpContext context, RefreshOutcome outcome)
        {
            if (!_diagnostics.EnableRefreshHeaders)
                return;

            context.Response.Headers["X-UAuth-Refresh"] = outcome switch
            {
                RefreshOutcome.NoOp => "no-op",
                RefreshOutcome.Touched => "touched",
                RefreshOutcome.ReauthRequired => "reauth-required",
                _ => "unknown"
            };
        }

    }
}
