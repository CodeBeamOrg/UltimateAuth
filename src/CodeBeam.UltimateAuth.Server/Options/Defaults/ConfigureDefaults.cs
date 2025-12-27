using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Contracts;

namespace CodeBeam.UltimateAuth.Server.Options
{
    internal class ConfigureDefaults
    {
        internal static void ApplyClientProfileDefaults(UAuthServerOptions o)
        {
            if (o.ClientProfile == UAuthClientProfile.NotSpecified)
            {
                o.Mode ??= UAuthMode.Hybrid;
                return;
            }

            if (o.Mode is null)
            {
                o.Mode = o.ClientProfile switch
                {
                    UAuthClientProfile.BlazorServer => UAuthMode.PureOpaque,
                    UAuthClientProfile.BlazorWasm => UAuthMode.SemiHybrid,
                    UAuthClientProfile.Maui => UAuthMode.SemiHybrid,
                    UAuthClientProfile.Mvc => UAuthMode.Hybrid,
                    UAuthClientProfile.Api => UAuthMode.PureJwt,
                    _ => throw new InvalidOperationException("Unsupported client profile. Please specify a client profile or make sure it's set NotSpecified")
                };
            }

            if (o.HubDeploymentMode == default)
            {
                o.HubDeploymentMode = o.ClientProfile switch
                {
                    UAuthClientProfile.BlazorWasm => UAuthHubDeploymentMode.Integrated,
                    UAuthClientProfile.Maui => UAuthHubDeploymentMode.Integrated,
                    _ => UAuthHubDeploymentMode.Embedded
                };
            }
        }

        internal static void ApplyModeDefaults(UAuthServerOptions o)
        {
            switch (o.Mode)
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
                    throw new InvalidOperationException($"Unsupported UAuthMode: {o.Mode}");
            }
        }

        internal static void ApplyAuthResponseDefaults(UAuthServerOptions o)
        {
            var ar = o.AuthResponse;
            if (ar is null)
                return;

            bool sessionNotSet = ar.SessionIdDelivery.Mode == TokenResponseMode.None;
            bool accessNotSet = ar.AccessTokenDelivery.Mode == TokenResponseMode.None;
            bool refreshNotSet = ar.RefreshTokenDelivery.Mode == TokenResponseMode.None;

            if (!sessionNotSet || !accessNotSet || !refreshNotSet)
                return;

            switch (o.ClientProfile)
            {
                // TODO: Change NotSpecified option defaults. Should be same as BlazorWasm.
                case UAuthClientProfile.NotSpecified:
                    ar.SessionIdDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Cookie };
                    ar.AccessTokenDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Cookie };
                    ar.RefreshTokenDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.None };
                    ar.Login.RedirectEnabled = true;
                    ar.Logout.RedirectEnabled = true;
                    break;
                case UAuthClientProfile.BlazorServer:
                    ar.SessionIdDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Cookie };
                    ar.AccessTokenDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Cookie };
                    ar.RefreshTokenDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.None };
                    ar.Login.RedirectEnabled = true;
                    ar.Logout.RedirectEnabled = true;
                    break;

                case UAuthClientProfile.BlazorWasm:
                    ar.SessionIdDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Header, HeaderFormat = HeaderTokenFormat.Bearer };
                    ar.AccessTokenDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Header, HeaderFormat = HeaderTokenFormat.Bearer };
                    ar.RefreshTokenDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Cookie };
                    ar.Login.RedirectEnabled = true;
                    ar.Logout.RedirectEnabled = true;
                    break;

                case UAuthClientProfile.Maui:
                    ar.SessionIdDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Header, HeaderFormat = HeaderTokenFormat.Bearer };
                    ar.AccessTokenDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Header, HeaderFormat = HeaderTokenFormat.Bearer };
                    ar.RefreshTokenDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Header, HeaderFormat = HeaderTokenFormat.Bearer };
                    ar.Login.RedirectEnabled = true;
                    ar.Logout.RedirectEnabled = true;
                    break;

                case UAuthClientProfile.Mvc:
                    ar.SessionIdDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Header, HeaderFormat = HeaderTokenFormat.Bearer };
                    ar.AccessTokenDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Header, HeaderFormat = HeaderTokenFormat.Bearer };
                    ar.RefreshTokenDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Cookie };
                    ar.Login.RedirectEnabled = true;
                    ar.Logout.RedirectEnabled = true;
                    break;

                case UAuthClientProfile.Api:
                    ar.SessionIdDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Header, HeaderFormat = HeaderTokenFormat.Bearer };
                    ar.AccessTokenDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Header, HeaderFormat = HeaderTokenFormat.Bearer };
                    ar.RefreshTokenDelivery = new CredentialResponseOptions() { Mode = TokenResponseMode.Header, HeaderFormat = HeaderTokenFormat.Bearer };
                    ar.Login.RedirectEnabled = false;
                    ar.Logout.RedirectEnabled = false;
                    break;
            }
        }

        private static void ApplyPureOpaqueDefaults(UAuthServerOptions o)
        {
            var s = o.Session;
            var t = o.Tokens;

            // Session behavior
            s.SlidingExpiration = true;

            // Default: long-lived idle session (UX friendly)
            s.IdleTimeout ??= TimeSpan.FromDays(7);

            // Hard re-auth boundary is an advanced security feature
            // Do NOT enable by default
            s.MaxLifetime ??= null;

            // SessionId is the primary opaque token, carried via cookie
            t.IssueJwt = false;

            // No separate opaque access token is issued outside the session cookie
            t.IssueOpaque = false;

            // Refresh token does not exist in PureOpaque
            t.IssueRefresh = false;
        }

        private static void ApplyHybridDefaults(UAuthServerOptions o)
        {
            var s = o.Session;
            var t = o.Tokens;

            s.SlidingExpiration = true;

            t.IssueJwt = true;
            t.IssueOpaque = true;
            t.AccessTokenLifetime = TimeSpan.FromMinutes(10);
            t.RefreshTokenLifetime = TimeSpan.FromDays(7);
        }

        private static void ApplySemiHybridDefaults(UAuthServerOptions o)
        {
            var s = o.Session;
            var t = o.Tokens;
            var p = o.Pkce;

            s.SlidingExpiration = false;

            t.IssueJwt = true;
            t.IssueOpaque = true;
            t.AccessTokenLifetime = TimeSpan.FromMinutes(10);
            t.RefreshTokenLifetime = TimeSpan.FromDays(7);
            t.AddJwtIdClaim = true;
        }

        private static void ApplyPureJwtDefaults(UAuthServerOptions o)
        {
            var t = o.Tokens;
            var p = o.Pkce;

            o.Session.SlidingExpiration = false;
            o.Session.IdleTimeout = null;
            o.Session.MaxLifetime = null;

            t.IssueJwt = true;
            t.IssueOpaque = false;
            t.AccessTokenLifetime = TimeSpan.FromMinutes(10);
            t.RefreshTokenLifetime = TimeSpan.FromDays(7);
            t.AddJwtIdClaim = true;
        }

    }
}
