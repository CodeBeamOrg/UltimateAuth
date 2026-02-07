using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Cookies;

internal sealed class UAuthCookiePolicyBuilder : IUAuthCookiePolicyBuilder
{
    public CookieOptions Build(CredentialResponseOptions response, AuthFlowContext context, CredentialKind kind)
    {
        if (response.Cookie is null)
            throw new InvalidOperationException("Cookie policy requested but Cookie options are null.");

        var src = response.Cookie;

        var options = new CookieOptions
        {
            HttpOnly = src.HttpOnly,
            Secure = src.SecurePolicy == CookieSecurePolicy.Always,
            Path = src.Path,
            Domain = src.Domain,
            SameSite = ResolveSameSite(src, context)
        };

        ApplyLifetime(options, src, context, kind);

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

    private static void ApplyLifetime(CookieOptions target, UAuthCookieOptions src, AuthFlowContext context, CredentialKind kind)
    {
        var buffer = src.Lifetime.IdleBuffer ?? TimeSpan.Zero;
        var baseLifetime = ResolveBaseLifetime(context, kind, src);

        if (baseLifetime is not null)
        {
            target.MaxAge = baseLifetime.Value + buffer;
        }
    }

    private static TimeSpan? ResolveBaseLifetime(AuthFlowContext context, CredentialKind kind, UAuthCookieOptions src)
    {
        if (src.MaxAge is not null)
            return src.MaxAge;

        if (src.Lifetime.AbsoluteLifetimeOverride is not null)
            return src.Lifetime.AbsoluteLifetimeOverride;

        return kind switch
        {
            CredentialKind.Session => ResolveSessionLifetime(context),
            CredentialKind.RefreshToken => context.EffectiveOptions.Options.Token.RefreshTokenLifetime,
            CredentialKind.AccessToken => context.EffectiveOptions.Options.Token.AccessTokenLifetime,
            _ => null
        };
    }

    private static TimeSpan? ResolveSessionLifetime(AuthFlowContext context)
    {
        var sessionIdle = context.EffectiveOptions.Options.Session.IdleTimeout;
        var refresh = context.EffectiveOptions.Options.Token.RefreshTokenLifetime;

        return context.EffectiveMode switch
        {
            UAuthMode.PureOpaque => sessionIdle,
            UAuthMode.Hybrid => Max(sessionIdle, refresh),
            _ => sessionIdle
        };
    }

    private static TimeSpan? Max(TimeSpan? a, TimeSpan? b)
    {
        if (a is null) return b;
        if (b is null) return a;
        return a > b ? a : b;
    }
}
