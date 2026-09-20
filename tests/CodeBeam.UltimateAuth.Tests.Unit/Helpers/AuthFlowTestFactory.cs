using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal static class AuthFlowTestFactory
{
    public static AuthFlowContext LoginSuccess(
        ReturnUrlInfo? returnUrlInfo = null,
        EffectiveRedirectResponse? redirect = null)
        => New(
            returnUrlInfo: returnUrlInfo,
            redirect: redirect);

    public static AuthFlowContext New(
        ReturnUrlInfo? returnUrlInfo = null,
        EffectiveRedirectResponse? redirect = null,
        TenantKey? tenant = null,
        UserKey? userKey = null,
        SessionSecurityContext? session = null,
        bool isAuthenticated = true,
        EffectiveAuthResponse? response = null)
    {
        return new AuthFlowContext(
            flowType: AuthFlowType.Login,
            clientProfile: UAuthClientProfile.BlazorServer,
            effectiveMode: UAuthMode.PureOpaque,
            device: TestDevice.Default(),
            tenantKey: tenant ?? TenantKey.Single,
            isAuthenticated: isAuthenticated,
            userKey: userKey ?? (isAuthenticated ? UserKey.New() : null),
            session: session,
            originalOptions: TestServerOptions.Default(),
            effectiveOptions: TestServerOptions.Effective(),
            response: response ?? new EffectiveAuthResponse(
                sessionIdDelivery:
                    CredentialResponseOptions.Disabled(GrantKind.Session),
                accessTokenDelivery:
                    CredentialResponseOptions.Disabled(GrantKind.AccessToken),
                refreshTokenDelivery:
                    CredentialResponseOptions.Disabled(GrantKind.RefreshToken),
                redirect:
                    redirect ?? EffectiveRedirectResponse.Disabled
            ),
            primaryTokenKind: PrimaryTokenKind.Session,
            returnUrlInfo: returnUrlInfo ?? ReturnUrlInfo.None()
        );
    }
}
