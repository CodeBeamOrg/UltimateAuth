using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class AuthResponseResolver : IAuthResponseResolver
{
    private readonly AuthResponseOptionsModeTemplateResolver _template;
    private readonly ClientProfileAuthResponseAdapter _adapter;

    public AuthResponseResolver(AuthResponseOptionsModeTemplateResolver template, ClientProfileAuthResponseAdapter adapter)
    {
        _template = template;
        _adapter = adapter;
    }

    public EffectiveAuthResponse Resolve(UAuthMode effectiveMode, AuthFlowType flowType, UAuthClientProfile clientProfile, EffectiveUAuthServerOptions effectiveOptions)
    {
        var template = _template.Resolve(effectiveMode, flowType);
        var adapted = _adapter.Adapt(template, clientProfile, effectiveMode, effectiveOptions);

        var bound = BindCookies(adapted, effectiveOptions.Options);
        // TODO: This is currently implicit
        Validate(bound);

        return new EffectiveAuthResponse(
            bound.SessionIdDelivery,
            bound.AccessTokenDelivery,
            bound.RefreshTokenDelivery,

            new EffectiveLoginRedirectResponse(
                bound.Login.RedirectEnabled,
                bound.Login.SuccessRedirect,
                bound.Login.FailureRedirect,
                bound.Login.FailureQueryKey,
                bound.Login.CodeQueryKey,
                bound.Login.FailureCodes
            ),

            new EffectiveLogoutRedirectResponse(
                bound.Logout.RedirectEnabled,
                bound.Logout.RedirectUrl,
                bound.Logout.AllowReturnUrlOverride
            )
        );
    }

    private static UAuthResponseOptions BindCookies(UAuthResponseOptions response, UAuthServerOptions server)
    {
        return new UAuthResponseOptions
        {
            SessionIdDelivery = Bind(response.SessionIdDelivery, server),
            AccessTokenDelivery = Bind(response.AccessTokenDelivery, server),
            RefreshTokenDelivery = Bind(response.RefreshTokenDelivery, server),
            Login = response.Login,
            Logout = response.Logout
        };
    }

    private static CredentialResponseOptions Bind(CredentialResponseOptions delivery, UAuthServerOptions server)
    {
        if (delivery.Mode != TokenResponseMode.Cookie)
            return delivery;

        var cookie = delivery.Kind switch
        {
            CredentialKind.Session => server.Cookie.Session,
            CredentialKind.AccessToken => server.Cookie.AccessToken,
            CredentialKind.RefreshToken => server.Cookie.RefreshToken,
            _ => throw new InvalidOperationException($"Unsupported credential kind: {delivery.Kind}")
        };

        return delivery.WithCookie(cookie);
    }

    private static void Validate(UAuthResponseOptions response)
    {
        ValidateDelivery(response.SessionIdDelivery);
        ValidateDelivery(response.AccessTokenDelivery);
        ValidateDelivery(response.RefreshTokenDelivery);
    }

    private static void ValidateDelivery(CredentialResponseOptions delivery)
    {
        if (delivery.Mode == TokenResponseMode.Cookie && delivery.Cookie is null)
        {
            throw new InvalidOperationException($"Credential '{delivery.Kind}' is configured as Cookie but no cookie options were bound.");
        }
    }

}
