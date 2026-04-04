using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

internal class ConfigureDefaults
{
    internal static void ApplyModeDefaults(UAuthMode effectiveMode, UAuthServerOptions o)
    {
        switch (effectiveMode)
        {
            case UAuthMode.PureOpaque:
                ApplyPureOpaqueDefaults(o);
                break;

            case UAuthMode.Hybrid:
                ApplyHybridDefaults(o);
                break;

            case UAuthMode.SemiHybrid:
                ApplySemiHybridDefaults(o);
                break;

            case UAuthMode.PureJwt:
                ApplyPureJwtDefaults(o);
                break;

            default:
                throw new InvalidOperationException($"Unsupported UAuthMode: {effectiveMode}");
        }
    }

    private static void ApplyPureOpaqueDefaults(UAuthServerOptions o)
    {
        var s = o.Session;
        var t = o.Token;
        var c = o.Cookie;
        var r = o.AuthResponse;

        // Session behavior
        s.SlidingExpiration = true;

        // Default: long-lived idle session (UX friendly)
        s.IdleTimeout ??= TimeSpan.FromDays(7);

        s.TouchInterval ??= TimeSpan.FromDays(1);

        // Hard re-auth boundary is an advanced security feature
        // Do NOT enable by default
        s.MaxLifetime ??= null;
        s.DeviceMismatchBehavior = DeviceMismatchBehavior.Allow;

        // SessionId is the primary opaque token, carried via cookie
        t.IssueJwt = false;

        // No separate opaque access token is issued outside the session cookie
        t.IssueOpaque = false;

        // Refresh token does not exist in PureOpaque
        t.IssueRefresh = false;

        c.Session.Lifetime.IdleBuffer = TimeSpan.FromDays(2);

        r.RefreshTokenDelivery = new CredentialResponseOptions
        {
            Mode = TokenResponseMode.None,
            TokenFormat = TokenFormat.Opaque
        };
    }

    private static void ApplyHybridDefaults(UAuthServerOptions o)
    {
        var s = o.Session;
        var t = o.Token;
        var c = o.Cookie;
        var r = o.AuthResponse;

        s.SlidingExpiration = true;
        s.TouchInterval = null;

        t.IssueJwt = true;
        t.IssueOpaque = true;
        t.AccessTokenLifetime = TimeSpan.FromMinutes(10);
        t.RefreshTokenLifetime = TimeSpan.FromDays(7);

        c.Session.Lifetime.IdleBuffer = TimeSpan.FromMinutes(5);
        c.RefreshToken.Lifetime.IdleBuffer = TimeSpan.FromMinutes(5);

        r.RefreshTokenDelivery = new CredentialResponseOptions
        {
            Mode = TokenResponseMode.Cookie,
            TokenFormat = TokenFormat.Opaque
        };
    }

    private static void ApplySemiHybridDefaults(UAuthServerOptions o)
    {
        var s = o.Session;
        var t = o.Token;
        var p = o.Pkce;
        var c = o.Cookie;

        s.SlidingExpiration = false;
        s.TouchInterval = null;

        t.IssueJwt = true;
        t.IssueOpaque = true;
        t.AccessTokenLifetime = TimeSpan.FromMinutes(10);
        t.RefreshTokenLifetime = TimeSpan.FromDays(7);
        t.AddJwtIdClaim = true;

        c.AccessToken.Lifetime.IdleBuffer = TimeSpan.FromMinutes(5);
        c.RefreshToken.Lifetime.IdleBuffer = TimeSpan.FromMinutes(5);
    }

    private static void ApplyPureJwtDefaults(UAuthServerOptions o)
    {
        var s = o.Session;
        var t = o.Token;
        var p = o.Pkce;
        var c = o.Cookie;

        s.TouchInterval = null;

        o.Session.SlidingExpiration = false;
        o.Session.IdleTimeout = null;
        o.Session.MaxLifetime = null;

        t.IssueJwt = true;
        t.IssueOpaque = false;
        t.AccessTokenLifetime = TimeSpan.FromMinutes(10);
        t.RefreshTokenLifetime = TimeSpan.FromDays(7);
        t.AddJwtIdClaim = true;

        c.AccessToken.Lifetime.IdleBuffer = TimeSpan.FromMinutes(5);
        c.RefreshToken.Lifetime.IdleBuffer = TimeSpan.FromMinutes(5);
    }

}
