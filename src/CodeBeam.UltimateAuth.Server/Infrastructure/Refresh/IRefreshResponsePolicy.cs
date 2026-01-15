using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface IRefreshResponsePolicy
    {
        CredentialKind SelectPrimary(AuthFlowContext flow, RefreshFlowRequest request, RefreshFlowResult result);
        bool WriteRefreshToken(AuthFlowContext flow);
    }
}
