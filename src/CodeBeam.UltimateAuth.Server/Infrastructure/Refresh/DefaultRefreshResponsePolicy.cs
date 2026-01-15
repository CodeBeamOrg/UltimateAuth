using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal class DefaultRefreshResponsePolicy : IRefreshResponsePolicy
    {
        public CredentialKind SelectPrimary(AuthFlowContext flow, RefreshFlowRequest request, RefreshFlowResult result)
        {
            if (flow.EffectiveMode == UAuthMode.PureOpaque)
                return CredentialKind.Session;

            if (flow.EffectiveMode == UAuthMode.PureJwt)
                return CredentialKind.AccessToken;

            if (!string.IsNullOrWhiteSpace(request.RefreshToken) && request.SessionId == null)
            {
                return CredentialKind.AccessToken;
            }

            if (request.SessionId != null)
            {
                return CredentialKind.Session;
            }

            if (flow.ClientProfile == UAuthClientProfile.Api)
                return CredentialKind.AccessToken;

            return CredentialKind.Session;
        }


        public bool WriteRefreshToken(AuthFlowContext flow)
        {
            if (flow.EffectiveMode != UAuthMode.PureOpaque)
                return true;

            return false;
        }

    }
}
