using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Diagnostics;

internal static class UAuthStartupDiagnostics
{
    public static IEnumerable<UAuthDiagnostic> Analyze(UAuthServerOptions options)
    {
        foreach (var d in AnalyzeCookies(options))
            yield return d;
    }

    private static IEnumerable<UAuthDiagnostic> AnalyzeCookies(UAuthServerOptions options)
    {
        if (options.HubDeploymentMode != UAuthHubDeploymentMode.External)
            yield break;

        var session = options.Cookie.Session;

        if (session.SameSite == SameSiteMode.None &&
            session.SecurePolicy != CookieSecurePolicy.Always)
        {
            yield return new UAuthDiagnostic(
                code: "UAUTH001",
                message:
                    "Session cookie uses SameSite=None without Secure in External deployment. " +
                    "This is insecure and may expose authentication to network attackers.",
                severity: UAuthDiagnosticSeverity.Error);
        }

        var refresh = options.Cookie.RefreshToken;

        if (refresh.SameSite == SameSiteMode.None &&
            refresh.SecurePolicy != CookieSecurePolicy.Always)
        {
            yield return new UAuthDiagnostic(
                code: "UAUTH002",
                message:
                    "Refresh token cookie uses SameSite=None without Secure in External deployment. " +
                    "This is a critical security risk and MUST NOT be used outside development.",
                severity: UAuthDiagnosticSeverity.Error);
        }

        // TODO: Think again with MAUI.
        if (!refresh.HttpOnly)
        {
            yield return new UAuthDiagnostic(
                code: "UAUTH003",
                message:
                    "Refresh token cookie is not HttpOnly. This allows JavaScript access and " +
                    "significantly increases the impact of XSS vulnerabilities.",
                severity: UAuthDiagnosticSeverity.Warning);
        }
    }
}

