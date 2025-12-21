using CodeBeam.UltimateAuth.Core;

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

        private static void ApplyPureOpaqueDefaults(UAuthServerOptions o)
        {
            var s = o.Session;
            var t = o.Tokens;

            s.SlidingExpiration = true;
            s.IdleTimeout ??= TimeSpan.FromHours(1);
            s.MaxLifetime ??= TimeSpan.FromDays(7);

            t.IssueJwt = false;
            t.IssueOpaque = false;
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
