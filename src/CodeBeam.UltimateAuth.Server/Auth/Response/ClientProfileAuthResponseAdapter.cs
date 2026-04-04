using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class ClientProfileAuthResponseAdapter
{
    public UAuthResponseOptions Adapt(UAuthResponseOptions template, UAuthClientProfile clientProfile, UAuthMode effectiveMode, EffectiveUAuthServerOptions effectiveOptions)
    {
        var configured = effectiveOptions.Options.AuthResponse;

        return new UAuthResponseOptions
        {
            SessionIdDelivery = AdaptCredential(template.SessionIdDelivery, GrantKind.Session, clientProfile),
            AccessTokenDelivery = AdaptCredential(template.AccessTokenDelivery, GrantKind.AccessToken, clientProfile),
            RefreshTokenDelivery = AdaptCredential(template.RefreshTokenDelivery, GrantKind.RefreshToken, clientProfile),

            Login = MergeLogin(template.Login, configured.Login),
            Logout = MergeLogout(template.Logout, configured.Logout)
        };
    }

    // NOTE:
    // effectiveMode and effectiveOptions are intentionally passed
    // to keep this adapter policy-extensible.
    // They will be used for future mode/option based response enforcement.
    private static CredentialResponseOptions AdaptCredential(CredentialResponseOptions original, GrantKind kind, UAuthClientProfile clientProfile)
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

    private static LoginRedirectOptions MergeLogin(LoginRedirectOptions template, LoginRedirectOptions configured)
    {
        return new LoginRedirectOptions
        {
            RedirectEnabled = configured.RedirectEnabled,
            SuccessRedirect = configured.SuccessRedirect ?? template.SuccessRedirect,
            FailureRedirect = configured.FailureRedirect ?? template.FailureRedirect,
            FailureQueryKey = configured.FailureQueryKey ?? template.FailureQueryKey,
            FailureCodes = configured.FailureCodes.Count > 0
                ? new Dictionary<AuthFailureReason, string>(configured.FailureCodes)
                : new Dictionary<AuthFailureReason, string>(template.FailureCodes),
            AllowReturnUrlOverride = configured.AllowReturnUrlOverride
        };
    }

    private static LogoutRedirectOptions MergeLogout(LogoutRedirectOptions template, LogoutRedirectOptions configured)
    {
        return new LogoutRedirectOptions
        {
            RedirectEnabled = configured.RedirectEnabled,
            RedirectUrl = configured.RedirectUrl ?? template.RedirectUrl,
            AllowReturnUrlOverride = configured.AllowReturnUrlOverride
        };
    }
}
