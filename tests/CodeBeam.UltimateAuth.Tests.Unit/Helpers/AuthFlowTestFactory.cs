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
    public static AuthFlowContext LoginSuccess(ReturnUrlInfo? returnUrlInfo = null, EffectiveRedirectResponse? redirect = null)
    {
        return new AuthFlowContext(
            flowType: AuthFlowType.Login,
            clientProfile: UAuthClientProfile.BlazorServer,
            effectiveMode: UAuthMode.PureOpaque,
            device: TestDevice.Default(),
            tenantKey: TenantKey.Single,
            isAuthenticated: true,
            userKey: UserKey.New(),
            session: null,
            originalOptions: TestServerOptions.Default(),
            effectiveOptions: TestServerOptions.Effective(),
            response: new EffectiveAuthResponse(
                sessionIdDelivery: CredentialResponseOptions.Disabled(CredentialKind.Session),
                accessTokenDelivery: CredentialResponseOptions.Disabled(CredentialKind.AccessToken),
                refreshTokenDelivery: CredentialResponseOptions.Disabled(CredentialKind.RefreshToken),
                redirect: redirect ?? EffectiveRedirectResponse.Disabled
            ),
            primaryTokenKind: PrimaryTokenKind.Session,
            returnUrlInfo: returnUrlInfo ?? ReturnUrlInfo.None()
        );
    }
}
