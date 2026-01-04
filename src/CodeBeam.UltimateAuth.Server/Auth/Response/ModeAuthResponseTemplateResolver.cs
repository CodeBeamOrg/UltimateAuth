using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    internal sealed class ModeAuthResponseTemplateResolver
    {
        public AuthResponseOptions Resolve(
            UAuthMode mode,
            AuthFlowType flowType)
        {
            return mode switch
            {
                UAuthMode.PureOpaque => PureOpaque(flowType),
                UAuthMode.Hybrid => Hybrid(flowType),
                UAuthMode.SemiHybrid => SemiHybrid(flowType),
                UAuthMode.PureJwt => PureJwt(flowType),
                _ => throw new InvalidOperationException($"Unsupported mode: {mode}")
            };
        }

        private static AuthResponseOptions PureOpaque(AuthFlowType flow)
            => new()
            {
                SessionIdDelivery = new()
                {
                    TokenFormat = TokenFormat.Opaque,
                    Mode = TokenResponseMode.Cookie,
                },
                AccessTokenDelivery = new()
                {
                    TokenFormat = TokenFormat.Opaque,
                    Mode = TokenResponseMode.None
                },
                RefreshTokenDelivery = new()
                {
                    TokenFormat = TokenFormat.Opaque,
                    Mode = TokenResponseMode.None
                },
                Login = { RedirectEnabled = true },
                Logout = { RedirectEnabled = true }
            };

        private static AuthResponseOptions Hybrid(AuthFlowType flow)
            => new()
            {
                SessionIdDelivery = new()
                {
                    TokenFormat = TokenFormat.Opaque,
                    Mode = TokenResponseMode.Cookie
                },
                AccessTokenDelivery = new()
                {
                    TokenFormat = TokenFormat.Jwt,
                    Mode = TokenResponseMode.Header
                },
                RefreshTokenDelivery = new()
                {
                    TokenFormat = TokenFormat.Opaque,
                    Mode = TokenResponseMode.Cookie
                },
                Login = { RedirectEnabled = true },
                Logout = { RedirectEnabled = true }
            };

        private static AuthResponseOptions SemiHybrid(AuthFlowType flow)
            => new()
            {
                SessionIdDelivery = new()
                {
                    TokenFormat = TokenFormat.Opaque,
                    Mode = TokenResponseMode.None
                },
                AccessTokenDelivery = new()
                {
                    TokenFormat = TokenFormat.Jwt,
                    Mode = TokenResponseMode.Header
                },
                RefreshTokenDelivery = new()
                {
                    TokenFormat = TokenFormat.Opaque,
                    Mode = TokenResponseMode.Header
                },
                Login = { RedirectEnabled = true },
                Logout = { RedirectEnabled = true }
            };

        private static AuthResponseOptions PureJwt(AuthFlowType flow)
            => new()
            {
                SessionIdDelivery = new()
                {
                    TokenFormat = TokenFormat.Opaque,
                    Mode = TokenResponseMode.None
                },
                AccessTokenDelivery = new()
                {
                    TokenFormat = TokenFormat.Jwt,
                    Mode = TokenResponseMode.Header
                },
                RefreshTokenDelivery = new()
                {
                    TokenFormat = TokenFormat.Opaque,
                    Mode = TokenResponseMode.Header
                },
                Login = { RedirectEnabled = true },
                Logout = { RedirectEnabled = true }
            };
    }
}
