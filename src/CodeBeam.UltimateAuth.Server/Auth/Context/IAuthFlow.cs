using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    public interface IAuthFlow
    {
        AuthFlowContext Begin(AuthFlowType flowType);
    }
}
