using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Flows;

internal class RefreshResponsePolicy : IRefreshResponsePolicy
{
    public GrantKind SelectPrimary(AuthFlowContext flow, RefreshFlowRequest request, RefreshFlowResult result)
    {
        if (flow.EffectiveMode == UAuthMode.PureOpaque)
            return GrantKind.Session;

        if (flow.EffectiveMode == UAuthMode.PureJwt)
            return GrantKind.AccessToken;

        if (!string.IsNullOrWhiteSpace(request.RefreshToken) && request.SessionId == null)
        {
            return GrantKind.AccessToken;
        }

        if (request.SessionId != null)
        {
            return GrantKind.Session;
        }

        if (flow.ClientProfile == UAuthClientProfile.Api)
            return GrantKind.AccessToken;

        return GrantKind.Session;
    }


    public bool WriteRefreshToken(AuthFlowContext flow)
    {
        if (flow.EffectiveMode != UAuthMode.PureOpaque)
            return true;

        return false;
    }

}
