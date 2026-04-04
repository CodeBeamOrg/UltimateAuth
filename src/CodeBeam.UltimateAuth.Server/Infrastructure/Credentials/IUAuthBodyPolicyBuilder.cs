using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface IUAuthBodyPolicyBuilder
{
    object BuildBodyValue(string rawValue, CredentialResponseOptions response, AuthFlowContext context);
}
