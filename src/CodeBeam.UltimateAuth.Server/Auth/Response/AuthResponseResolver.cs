using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class AuthResponseResolver : IAuthResponseResolver
{
    public EffectiveAuthResponse Resolve(UAuthMode effectiveMode, AuthFlowType flowType, UAuthClientProfile clientProfile, EffectiveUAuthServerOptions effectiveOptions)
    {
        var options = effectiveOptions.Options;

        var redirect = flowType switch
        {
            AuthFlowType.Login or AuthFlowType.Reauthentication =>
                EffectiveRedirectResponse.FromLogin(options.Redirect.Login),

            AuthFlowType.Logout =>
                EffectiveRedirectResponse.FromLogout(options.Redirect.Logout),

            _ => EffectiveRedirectResponse.Disabled
        };

        return ResolveCore(effectiveMode, clientProfile, effectiveOptions, redirect);
    }

    public EffectiveAuthResponse ResolveForAuthentication(UAuthMode effectiveMode, UAuthClientProfile clientProfile, EffectiveUAuthServerOptions effectiveOptions)
    {
        return ResolveCore(effectiveMode, clientProfile, effectiveOptions, EffectiveRedirectResponse.Disabled);
    }

    private EffectiveAuthResponse ResolveCore(UAuthMode effectiveMode, UAuthClientProfile clientProfile, EffectiveUAuthServerOptions effectiveOptions, EffectiveRedirectResponse redirect)
    {
        var options = effectiveOptions.Options;
        var delivery = options.GrantDelivery;

        var session = ResolveGrant(
            GrantKind.Session,
            TokenFormat.Opaque,
            effectiveMode is UAuthMode.PureOpaque or UAuthMode.Hybrid,
            effectiveMode is UAuthMode.PureOpaque or UAuthMode.Hybrid
                ? GrantDeliveryMode.Cookie
                : GrantDeliveryMode.None,
            delivery.Session,
            delivery.Default,
            clientProfile,
            options);

        var accessToken = ResolveGrant(
            GrantKind.AccessToken,
            effectiveMode == UAuthMode.PureOpaque
                ? TokenFormat.Opaque
                : TokenFormat.Jwt,
            effectiveMode is UAuthMode.Hybrid
                or UAuthMode.SemiHybrid
                or UAuthMode.PureJwt,
            effectiveMode == UAuthMode.PureOpaque
                ? GrantDeliveryMode.None
                : GrantDeliveryMode.Header,
            delivery.AccessToken,
            delivery.Default,
            clientProfile,
            options);

        var refreshToken = ResolveGrant(
            GrantKind.RefreshToken,
            TokenFormat.Opaque,
            effectiveMode is UAuthMode.Hybrid
                or UAuthMode.SemiHybrid
                or UAuthMode.PureJwt,
            effectiveMode == UAuthMode.Hybrid
                ? GrantDeliveryMode.Cookie
                : effectiveMode == UAuthMode.PureOpaque
                    ? GrantDeliveryMode.None
                    : GrantDeliveryMode.Header,
            delivery.RefreshToken,
            delivery.Default,
            clientProfile,
            options);

        return new EffectiveAuthResponse(
            session,
            accessToken,
            refreshToken,
            redirect);
    }

    private static EffectiveGrantDelivery ResolveGrant(
        GrantKind kind,
        TokenFormat tokenFormat,
        bool available,
        GrantDeliveryMode modeDefault,
        GrantDeliveryOptions configured,
        GrantDeliveryMode? globalDefault,
        UAuthClientProfile clientProfile,
        UAuthServerOptions server)
    {
        if (!available)
        {
            if (configured.Mode is not null &&
                configured.Mode != GrantDeliveryMode.None)
            {
                throw new InvalidOperationException(
                    $"Grant '{kind}' is not available in the selected authentication mode.");
            }

            return EffectiveGrantDelivery.Disabled(kind);
        }

        var profileDefault = clientProfile == UAuthClientProfile.Maui &&
                             modeDefault == GrantDeliveryMode.Cookie
            ? GrantDeliveryMode.Header
            : modeDefault;

        var mode = configured.Mode
                   ?? globalDefault
                   ?? profileDefault;

        if (!Enum.IsDefined(mode))
        {
            throw new InvalidOperationException(
                $"Invalid delivery mode '{mode}' for grant '{kind}'.");
        }

        if (mode == GrantDeliveryMode.None)
        {
            throw new InvalidOperationException(
                $"Grant '{kind}' is required by the selected authentication mode " +
                "and cannot have delivery mode None.");
        }

        if (mode == GrantDeliveryMode.Body)
        {
            throw new NotSupportedException(
                $"Body grant delivery is not supported for '{kind}'.");
        }

        var cookie = mode == GrantDeliveryMode.Cookie
            ? kind switch
            {
                GrantKind.Session => server.Cookie.Session,
                GrantKind.AccessToken => server.Cookie.AccessToken,
                GrantKind.RefreshToken => server.Cookie.RefreshToken,
                _ => throw new InvalidOperationException(
                    $"Unsupported grant kind '{kind}'.")
            }
            : null;

        if (cookie is not null)
        {
            try
            {
                _ = new SetCookieHeaderValue(
                    cookie.Name,
                    "validation").ToString();
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    $"Invalid cookie name '{cookie.Name}' for grant '{kind}'.",
                    ex);
            }
        }

        var headerName = configured.HeaderName ?? kind switch
        {
            GrantKind.Session => "uas",
            GrantKind.AccessToken => "uat",
            GrantKind.RefreshToken => "uar",
            _ => throw new InvalidOperationException(
                $"Unsupported grant kind '{kind}'.")
        };

        return new EffectiveGrantDelivery(
            kind,
            tokenFormat,
            mode,
            headerName,
            configured.HeaderFormat ?? HeaderTokenFormat.Bearer,
            cookie);
    }
}
