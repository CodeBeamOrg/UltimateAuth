using CodeBeam.UltimateAuth.Core;

namespace CodeBeam.UltimateAuth.Server.Options;

internal static class ConfigureDefaults
{
    internal static void ApplyModeDefaults(UAuthMode effectiveMode, UAuthServerOptions options)
    {
        switch (effectiveMode)
        {
            case UAuthMode.PureOpaque:
                ApplyPureOpaqueDefaults(options);
                break;

            case UAuthMode.Hybrid:
                ApplyHybridDefaults(options);
                break;

            case UAuthMode.SemiHybrid:
                ApplySemiHybridDefaults(options);
                break;

            case UAuthMode.PureJwt:
                ApplyPureJwtDefaults(options);
                break;

            default:
                throw new InvalidOperationException($"Unsupported UAuthMode: {effectiveMode}");
        }
    }

    private static void ApplyPureOpaqueDefaults(UAuthServerOptions options)
    {
        var session = options.Session;
        var token = options.Token;
        var cookie = options.Cookie;

        session.IdleTimeout ??= TimeSpan.FromDays(7);
        session.TouchInterval ??= TimeSpan.FromDays(1);

        session.MaxLifetime ??= null;

        token.IssueJwt = false;
        token.IssueOpaque = false;
        token.IssueRefresh = false;

        cookie.Session.Lifetime.IdleBuffer = TimeSpan.FromDays(2);
    }

    private static void ApplyHybridDefaults(UAuthServerOptions options)
    {
        var session = options.Session;
        var token = options.Token;
        var cookie = options.Cookie;

        session.TouchInterval = null;

        token.IssueJwt = true;
        token.IssueOpaque = true;
        token.AccessTokenLifetime = TimeSpan.FromMinutes(10);
        token.RefreshTokenLifetime = TimeSpan.FromDays(7);

        cookie.Session.Lifetime.IdleBuffer = TimeSpan.FromMinutes(5);

        cookie.RefreshToken.Lifetime.IdleBuffer = TimeSpan.FromMinutes(5);
    }

    private static void ApplySemiHybridDefaults(UAuthServerOptions options)
    {
        var session = options.Session;
        var token = options.Token;
        var cookie = options.Cookie;

        session.SlidingExpiration = false;
        session.TouchInterval = null;

        token.IssueJwt = true;
        token.IssueOpaque = true;
        token.AccessTokenLifetime = TimeSpan.FromMinutes(10);
        token.RefreshTokenLifetime = TimeSpan.FromDays(7);
        token.AddJwtIdClaim = true;

        cookie.AccessToken.Lifetime.IdleBuffer = TimeSpan.FromMinutes(5);

        cookie.RefreshToken.Lifetime.IdleBuffer = TimeSpan.FromMinutes(5);
    }

    private static void ApplyPureJwtDefaults(UAuthServerOptions options)
    {
        var session = options.Session;
        var token = options.Token;
        var cookie = options.Cookie;

        // PureJwt does not use server-side sessions.
        session.SlidingExpiration = false;
        session.TouchInterval = null;
        session.IdleTimeout = null;
        session.MaxLifetime = null;

        token.IssueJwt = true;
        token.IssueOpaque = false;
        token.IssueRefresh = true;
        token.AccessTokenLifetime = TimeSpan.FromMinutes(10);
        token.RefreshTokenLifetime = TimeSpan.FromDays(7);
        token.AddJwtIdClaim = true;

        cookie.AccessToken.Lifetime.IdleBuffer = TimeSpan.FromMinutes(5);

        cookie.RefreshToken.Lifetime.IdleBuffer = TimeSpan.FromMinutes(5);
    }
}
