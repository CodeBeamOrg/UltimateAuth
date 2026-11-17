using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface IUAuthHeaderPolicyBuilder
{
    string BuildHeaderValue(string rawValue, EffectiveGrantDelivery delivery, AuthFlowContext context);
}
