using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class UAuthCookiePolicyBuilder : IUAuthCookiePolicyBuilder
{
    public CookieOptions Build(EffectiveGrantDelivery delivery, AuthFlowContext context, GrantKind kind)
    {
        if (delivery.Mode != GrantDeliveryMode.Cookie)
        {
            throw new InvalidOperationException($"Cookie policy requested for non-cookie grant '{kind}'.");
        }

        if (delivery.Kind != kind)
        {
            throw new InvalidOperationException($"Cookie grant kind mismatch: '{delivery.Kind}' and '{kind}'.");
        }

        var src = delivery.Cookie
            ?? throw new InvalidOperationException($"Cookie options missing for grant '{kind}'.");

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

        return context.Options.HubDeploymentMode switch
        {
            UAuthHubDeploymentMode.Embedded => SameSiteMode.Strict,
            UAuthHubDeploymentMode.Integrated => SameSiteMode.Lax,
            UAuthHubDeploymentMode.External => SameSiteMode.None,
            _ => SameSiteMode.Lax
        };
    }

    private static void ApplyLifetime(CookieOptions target, UAuthCookieOptions src, AuthFlowContext context, GrantKind kind)
    {
        var buffer = src.Lifetime.IdleBuffer ?? TimeSpan.Zero;
        var baseLifetime = ResolveBaseLifetime(context, kind, src);

        if (baseLifetime is not null)
        {
            target.MaxAge = baseLifetime.Value + buffer;
        }
    }

    private static TimeSpan? ResolveBaseLifetime(AuthFlowContext context, GrantKind kind, UAuthCookieOptions src)
    {
        if (src.MaxAge is not null)
            return src.MaxAge;

        if (src.Lifetime.AbsoluteLifetimeOverride is not null)
            return src.Lifetime.AbsoluteLifetimeOverride;

        return kind switch
        {
            GrantKind.Session => ResolveSessionLifetime(context),
            GrantKind.RefreshToken => context.Options.Token.RefreshTokenLifetime,
            GrantKind.AccessToken => context.Options.Token.AccessTokenLifetime,
            _ => null
        };
    }

    private static TimeSpan? ResolveSessionLifetime(AuthFlowContext context)
    {
        var sessionIdle = context.Options.Session.IdleTimeout;
        var refresh = context.Options.Token.RefreshTokenLifetime;

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
