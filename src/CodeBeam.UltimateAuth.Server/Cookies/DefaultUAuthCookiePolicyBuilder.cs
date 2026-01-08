using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Cookies;

internal sealed class DefaultUAuthCookiePolicyBuilder : IUAuthCookiePolicyBuilder
{
    public CookieOptions Build(CredentialResponseOptions response, AuthFlowContext context, TimeSpan? logicalLifetime)
    {
        if (response.Cookie is null)
            throw new InvalidOperationException("Cookie policy requested but Cookie options are null.");

        var src = response.Cookie;

        var options = new CookieOptions
        {
            HttpOnly = src.HttpOnly,
            Secure = src.SecurePolicy == CookieSecurePolicy.Always,
            Path = src.Path,
            Domain = src.Domain
        };

        options.SameSite = ResolveSameSite(src, context);
        ApplyLifetime(options, src, logicalLifetime);

        return options;
    }

    private static SameSiteMode ResolveSameSite(UAuthCookieOptions cookie, AuthFlowContext context)
    {
        if (cookie.SameSite is not null)
            return cookie.SameSite.Value;

        return context.OriginalOptions.HubDeploymentMode switch
        {
            UAuthHubDeploymentMode.Embedded => SameSiteMode.Strict,
            UAuthHubDeploymentMode.Integrated => SameSiteMode.Lax,
            UAuthHubDeploymentMode.External => SameSiteMode.None,
            _ => SameSiteMode.Lax
        };
    }

    private static void ApplyLifetime(CookieOptions target, UAuthCookieOptions src, TimeSpan? logicalLifetime)
    {
        var buffer = src.Lifetime.IdleBuffer ?? TimeSpan.Zero;
        TimeSpan? baseLifetime = null;

        // 1️⃣ Hard MaxAge override (base)
        if (src.MaxAge is not null)
        {
            baseLifetime = src.MaxAge;
        }
        // 2️⃣ Absolute lifetime override (base)
        else if (src.Lifetime.AbsoluteLifetimeOverride is not null)
        {
            baseLifetime = src.Lifetime.AbsoluteLifetimeOverride;
        }
        // 3️⃣ Logical lifetime (effective)
        else if (logicalLifetime is not null)
        {
            baseLifetime = logicalLifetime;
        }

        if (baseLifetime is not null)
        {
            target.MaxAge = baseLifetime.Value + buffer;
        }
    }
}
