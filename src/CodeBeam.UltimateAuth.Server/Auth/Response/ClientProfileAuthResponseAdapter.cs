using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class ClientProfileAuthResponseAdapter
{
    public AuthResponseOptions Adapt(AuthResponseOptions template, UAuthClientProfile clientProfile, UAuthMode effectiveMode, EffectiveUAuthServerOptions effectiveOptions)
    {
        return new AuthResponseOptions
        {
            SessionIdDelivery = AdaptCredential(template.SessionIdDelivery, CredentialKind.Session, clientProfile),
            AccessTokenDelivery = AdaptCredential(template.AccessTokenDelivery, CredentialKind.AccessToken, clientProfile),
            RefreshTokenDelivery = AdaptCredential(template.RefreshTokenDelivery, CredentialKind.RefreshToken, clientProfile),

            Login = template.Login,
            Logout = template.Logout
        };
    }

    // NOTE:
    // effectiveMode and effectiveOptions are intentionally passed
    // to keep this adapter policy-extensible.
    // They will be used for future mode/option based response enforcement.
    private static CredentialResponseOptions AdaptCredential(CredentialResponseOptions original, CredentialKind kind, UAuthClientProfile clientProfile)
    {
        if (clientProfile == UAuthClientProfile.Maui && original.Mode == TokenResponseMode.Cookie)
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
