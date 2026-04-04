using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal class UAuthBodyPolicyBuilder : IUAuthBodyPolicyBuilder
{
    public object BuildBodyValue(string rawValue, CredentialResponseOptions response, AuthFlowContext context)
    {
        throw new NotImplementedException();
    }
}
