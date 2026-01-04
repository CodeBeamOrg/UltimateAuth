using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

internal sealed class UAuthCookieOptionsBuilder
{
    private readonly UAuthServerOptions _server;

    public UAuthCookieOptionsBuilder(IOptions<UAuthServerOptions> server)
    {
        _server = server.Value;
    }

    /// <summary>
    /// Builds CookieOptions for a given cookie definition.
    /// logicalLifetime:
    /// - Session idle timeout
    /// - Refresh token lifetime
    /// - Access token lifetime
    /// </summary>
    public CookieOptions Build(
        UAuthCookieOptions src,
        TimeSpan? logicalLifetime)
    {
        var options = new CookieOptions
        {
            HttpOnly = src.HttpOnly,
            Secure = src.SecurePolicy == CookieSecurePolicy.Always,
            SameSite = ResolveSameSite(src),
            Path = src.Path
        };

        // 1️⃣ Hard MaxAge override (highest priority)
        if (src.MaxAge is not null)
        {
            options.MaxAge = src.MaxAge;
            return options;
        }

        // 2️⃣ Absolute lifetime override
        if (src.Lifetime.AbsoluteLifetimeOverride is not null)
        {
            options.MaxAge = src.Lifetime.AbsoluteLifetimeOverride;
            return options;
        }

        // 3️⃣ Logical lifetime + idle buffer
        if (logicalLifetime is not null)
        {
            var buffer = src.Lifetime.IdleBuffer ?? TimeSpan.Zero;
            options.MaxAge = logicalLifetime.Value + buffer;
        }

        return options;
    }

    private SameSiteMode ResolveSameSite(UAuthCookieOptions cookie)
    {
        if (cookie.SameSite is not null)
            return cookie.SameSite.Value;

        return _server.HubDeploymentMode switch
        {
            UAuthHubDeploymentMode.Embedded => SameSiteMode.Strict,
            UAuthHubDeploymentMode.Integrated => SameSiteMode.Lax,
            UAuthHubDeploymentMode.External => SameSiteMode.None,
            _ => SameSiteMode.Lax
        };
    }
}
