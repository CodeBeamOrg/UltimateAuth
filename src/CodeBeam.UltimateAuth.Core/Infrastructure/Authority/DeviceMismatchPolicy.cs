using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class DeviceMismatchPolicy : IAuthorityPolicy
    {
        public bool AppliesTo(AuthContext context)
            => context.Device is not null;

        public AccessDecisionResult Decide(AuthContext context)
        {
            var device = context.Device;

            //if (device.IsKnownDevice)
            //    return AuthorizationResult.Allow();

            return context.Operation switch
            {
                AuthOperation.Access =>
                    AccessDecisionResult.Deny("Access from unknown device."),

                AuthOperation.Refresh =>
                    AccessDecisionResult.Challenge("Device verification required."),

                AuthOperation.Login => AccessDecisionResult.Allow(), // login establishes device

                _ => AccessDecisionResult.Allow()
            };
        }
    }
}
