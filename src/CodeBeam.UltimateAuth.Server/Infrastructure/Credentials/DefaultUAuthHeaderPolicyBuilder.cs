using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal class DefaultUAuthHeaderPolicyBuilder : IUAuthHeaderPolicyBuilder
    {
        public string BuildHeaderValue(string rawValue, CredentialResponseOptions response, AuthFlowContext context)
        {
            throw new NotImplementedException();
        }
    }
}
