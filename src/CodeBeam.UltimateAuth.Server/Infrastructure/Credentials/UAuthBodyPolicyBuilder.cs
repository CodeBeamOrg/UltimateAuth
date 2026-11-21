using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal class UAuthBodyPolicyBuilder : IUAuthBodyPolicyBuilder
{
    public object BuildBodyValue(string rawValue, EffectiveGrantDelivery delivery, AuthFlowContext context)
    {
        throw new NotImplementedException();
    }
}
