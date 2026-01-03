using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    internal sealed class ClientProfileAuthResponseAdapter
    {
        public EffectiveAuthResponse Adapt(AuthResponseOptions template, AuthFlowContext ctx)
        {
            return new EffectiveAuthResponse(
                SessionId: AdaptCredential(template.SessionIdDelivery, ctx),
                AccessToken: AdaptCredential(template.AccessTokenDelivery, ctx),
                RefreshToken: AdaptCredential(template.RefreshTokenDelivery, ctx),
                LoginRedirectEnabled: template.Login.RedirectEnabled,
                LogoutRedirectEnabled: template.Logout.RedirectEnabled
            );
        }

        private static CredentialResponseOptions AdaptCredential(CredentialResponseOptions original, AuthFlowContext ctx)
        {
            if (ctx.ClientProfile == UAuthClientProfile.Maui && original.Mode == TokenResponseMode.Cookie)
            {
                return ToHeader(original);
            }

            if (original.TokenFormat == TokenFormat.Jwt && original.Mode == TokenResponseMode.Cookie)
            {
                return ToHeader(original);
            }

            return original;
        }

        private static CredentialResponseOptions ToHeader(CredentialResponseOptions original)
        {
            return new CredentialResponseOptions
            {
                TokenFormat = original.TokenFormat,
                Mode = TokenResponseMode.Header,
                HeaderFormat = HeaderTokenFormat.Bearer,
                Name = original.Name
            };
        }

    }
}
