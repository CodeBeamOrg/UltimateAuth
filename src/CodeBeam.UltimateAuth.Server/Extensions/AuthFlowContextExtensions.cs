using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class AuthFlowContextExtensions
    {
        public static AuthContext ToAuthContext(this AuthFlowContext flow, DateTimeOffset now)
        {
            return new AuthContext
            {
                Tenant = flow.Tenant,
                Operation = flow.FlowType.ToAuthOperation(),
                Mode = flow.EffectiveMode,
                At = now,
                Device = flow.Device,
                Session = flow.Session
            };
        }

        public static AuthFlowContext WithClientProfile(this AuthFlowContext flow, UAuthClientProfile profile)
        {
            return new AuthFlowContext(
                flow.FlowType,
                profile,
                flow.EffectiveMode,
                flow.Device,
                flow.Tenant,
                flow.IsAuthenticated,
                flow.UserKey,
                flow.Session,
                flow.OriginalOptions,
                flow.EffectiveOptions,
                flow.Response,
                flow.PrimaryTokenKind);
        }

    }
}
