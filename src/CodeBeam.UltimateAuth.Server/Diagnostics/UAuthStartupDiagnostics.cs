using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Diagnostics;

internal static class UAuthStartupDiagnostics
{
    // TODO: Add startup log
    public static IEnumerable<UAuthDiagnostic> Analyze(UAuthServerOptions options)
    {
        if (options.HubDeploymentMode == UAuthHubDeploymentMode.External && options.Cookie.SecurePolicy != CookieSecurePolicy.Always)
        {
            yield return new UAuthDiagnostic(
                "UAUTH001",
                "External UAuthHub without Secure cookies is unsafe. This should only be used for development or testing.",
                UAuthDiagnosticSeverity.Warning);
        }
    }
}
