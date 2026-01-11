using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class AuthFlowContextExtensions
    {
        public static AuthFlowContext WithClientProfile(this AuthFlowContext flow, UAuthClientProfile profile)
        {
            return new AuthFlowContext(
                flow.FlowType,
                profile,
                flow.EffectiveMode,
                flow.TenantId,
                flow.IsAuthenticated,
                flow.UserId,
                flow.SessionId,
                flow.OriginalOptions,
                flow.EffectiveOptions,
                flow.Response,
                flow.PrimaryTokenKind);
        }
    }
}
